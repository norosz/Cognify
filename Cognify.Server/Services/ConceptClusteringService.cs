using System.Globalization;
using System.Text.RegularExpressions;
using Cognify.Server.Data;
using Cognify.Server.Dtos.Concepts;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class ConceptClusteringService(ApplicationDbContext context, IUserContextService userContext) : IConceptClusteringService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "of", "to", "in", "on", "for", "with", "by", "at", "from",
        "is", "are", "was", "were", "be", "being", "been", "this", "that", "these", "those",
        "as", "into", "about", "over", "under", "between", "without"
    };

    public async Task<List<ConceptClusterDto>> GetConceptClustersAsync(Guid moduleId)
    {
        var module = await GetOwnedModuleAsync(moduleId);
        if (module == null)
        {
            return [];
        }

        var clusters = await context.ConceptClusters
            .AsNoTracking()
            .Include(c => c.Topics)
            .Where(c => c.ModuleId == moduleId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        return clusters.Select(MapToDto).ToList();
    }

    public async Task<List<ConceptClusterDto>> RefreshConceptClustersAsync(Guid moduleId, bool forceRegenerate = true)
    {
        var module = await GetOwnedModuleAsync(moduleId);
        if (module == null)
        {
            return [];
        }

        if (!forceRegenerate)
        {
            var existing = await context.ConceptClusters
                .AsNoTracking()
                .Include(c => c.Topics)
                .Where(c => c.ModuleId == moduleId)
                .ToListAsync();

            if (existing.Count > 0)
            {
                return existing.Select(MapToDto).ToList();
            }
        }

        var knowledgeStates = await context.UserKnowledgeStates
            .Include(s => s.User)
            .Where(s => s.UserId == userContext.GetCurrentUserId())
            .Join(context.Notes.AsNoTracking(),
                state => state.SourceNoteId,
                note => note.Id,
                (state, note) => new { state, note })
            .Where(x => x.note.ModuleId == moduleId)
            .ToListAsync();

        var topicItems = knowledgeStates
            .Where(x => !string.IsNullOrWhiteSpace(x.state.Topic))
            .Select(x => new TopicItem(x.state.Id, x.state.Topic, Tokenize(x.state.Topic)))
            .ToList();

        await ClearExistingClustersAsync(moduleId);

        if (topicItems.Count == 0)
        {
            return [];
        }

        var clusters = BuildClusters(topicItems);
        var now = DateTime.UtcNow;
        var createdClusters = new List<ConceptCluster>();

        foreach (var cluster in clusters)
        {
            var label = BuildLabel(cluster);
            var conceptCluster = new ConceptCluster
            {
                ModuleId = moduleId,
                Label = label,
                CreatedAt = now,
                UpdatedAt = now,
                Topics = cluster.Select(item => new ConceptTopic
                {
                    Topic = item.Topic,
                    CreatedAt = now
                }).ToList()
            };

            createdClusters.Add(conceptCluster);
        }

        context.ConceptClusters.AddRange(createdClusters);
        await context.SaveChangesAsync();

        foreach (var cluster in createdClusters)
        {
            var topicSet = cluster.Topics.Select(t => t.Topic).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var state in knowledgeStates.Where(s => topicSet.Contains(s.state.Topic)))
            {
                state.state.ConceptClusterId = cluster.Id;
                context.UserKnowledgeStates.Update(state.state);
            }
        }

        await context.SaveChangesAsync();

        return createdClusters.Select(MapToDto).ToList();
    }

    private async Task ClearExistingClustersAsync(Guid moduleId)
    {
        var clusters = await context.ConceptClusters
            .Include(c => c.Topics)
            .Where(c => c.ModuleId == moduleId)
            .ToListAsync();

        if (clusters.Count == 0)
        {
            return;
        }

        context.ConceptTopics.RemoveRange(clusters.SelectMany(c => c.Topics));
        context.ConceptClusters.RemoveRange(clusters);
        await context.SaveChangesAsync();
    }

    private async Task<Module?> GetOwnedModuleAsync(Guid moduleId)
    {
        var userId = userContext.GetCurrentUserId();
        return await context.Modules.FirstOrDefaultAsync(m => m.Id == moduleId && m.OwnerUserId == userId);
    }

    private static List<List<TopicItem>> BuildClusters(List<TopicItem> items)
    {
        var clusters = new List<List<TopicItem>>();
        var unassigned = new List<TopicItem>(items);

        while (unassigned.Count > 0)
        {
            var seed = unassigned[0];
            unassigned.RemoveAt(0);

            var cluster = new List<TopicItem> { seed };
            var remaining = new List<TopicItem>();

            foreach (var item in unassigned)
            {
                var similarity = ComputeSimilarity(seed, item);
                if (similarity >= 0.5 || seed.Topic.Contains(item.Topic, StringComparison.OrdinalIgnoreCase) || item.Topic.Contains(seed.Topic, StringComparison.OrdinalIgnoreCase))
                {
                    cluster.Add(item);
                }
                else
                {
                    remaining.Add(item);
                }
            }

            clusters.Add(cluster);
            unassigned = remaining;
        }

        return clusters;
    }

    private static double ComputeSimilarity(TopicItem left, TopicItem right)
    {
        if (left.Tokens.Count == 0 || right.Tokens.Count == 0)
        {
            return 0;
        }

        var intersection = left.Tokens.Intersect(right.Tokens).Count();
        var union = left.Tokens.Union(right.Tokens).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static string BuildLabel(List<TopicItem> cluster)
    {
        if (cluster.Count == 1)
        {
            return cluster[0].Topic;
        }

        var tokenCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in cluster)
        {
            foreach (var token in item.Tokens)
            {
                tokenCounts[token] = tokenCounts.TryGetValue(token, out var count) ? count + 1 : 1;
            }
        }

        var topTokens = tokenCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(2)
            .Select(kvp => ToTitle(kvp.Key))
            .ToList();

        if (topTokens.Count == 0)
        {
            return cluster[0].Topic;
        }

        return string.Join(" ", topTokens);
    }

    private static HashSet<string> Tokenize(string topic)
    {
        var tokens = Regex.Split(topic.ToLowerInvariant(), "[^a-z0-9]+")
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Where(token => !StopWords.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return tokens;
    }

    private static string ToTitle(string token)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(token);
    }

    private static ConceptClusterDto MapToDto(ConceptCluster cluster)
    {
        return new ConceptClusterDto
        {
            Id = cluster.Id,
            ModuleId = cluster.ModuleId,
            Label = cluster.Label,
            CreatedAt = cluster.CreatedAt,
            UpdatedAt = cluster.UpdatedAt,
            Topics = cluster.Topics.Select(t => t.Topic).ToList()
        };
    }

    private sealed record TopicItem(Guid StateId, string Topic, HashSet<string> Tokens);
}
