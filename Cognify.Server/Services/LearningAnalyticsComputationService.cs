using Cognify.Server.Data;
using Cognify.Server.Dtos.Analytics;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class LearningAnalyticsComputationService(ApplicationDbContext context, IDecayPredictionService decayService) : ILearningAnalyticsComputationService
{
    public async Task<LearningAnalyticsSummaryDto> GetSummaryAsync(Guid userId, bool includeExams)
    {
        var now = DateTime.UtcNow;

        var states = await context.UserKnowledgeStates
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var attempts = await GetAttemptSnapshotsAsync(userId, includeExams);
        var interactions = await GetInteractionsAsync(userId, includeExams);

        var totalTopics = states.Count;
        var averageMastery = totalTopics > 0 ? states.Average(s => s.MasteryScore) : 0;
        var dynamicRisks = states.Select(s => decayService.CalculateForgettingRisk(s.MasteryScore, s.LastReviewedAt, now)).ToList();
        var averageRisk = dynamicRisks.Count > 0 ? dynamicRisks.Average() : 0;
        var weakTopicsCount = states.Count(s => s.MasteryScore < 0.5 || decayService.CalculateForgettingRisk(s.MasteryScore, s.LastReviewedAt, now) > 0.6);

        var totalAttempts = attempts.Count;
        var interactionAccuracy = interactions.Count(i => i.IsCorrect.HasValue) > 0
            ? interactions.Where(i => i.IsCorrect.HasValue).Average(i => i.IsCorrect == true ? 1 : 0)
            : 0;

        var weightedAttemptAccuracy = CalculateWeightedAttemptAccuracy(attempts);
        var accuracyRate = interactionAccuracy > 0 ? interactionAccuracy : weightedAttemptAccuracy;

        var activityDates = new[]
        {
            attempts.Count > 0 ? attempts.Max(a => a.CreatedAt) : (DateTime?)null,
            interactions.Count > 0 ? interactions.Max(i => i.CreatedAt) : (DateTime?)null,
            states.Any(s => s.LastReviewedAt.HasValue)
                ? states.Where(s => s.LastReviewedAt.HasValue).Max(s => s.LastReviewedAt)
                : (DateTime?)null
        };

        DateTime? lastActivity = activityDates.Any(d => d.HasValue)
            ? activityDates.Where(d => d.HasValue).Select(d => d!.Value).Max()
            : null;

        // Avoid showing a misleading default readiness score (e.g., 10%) for brand new users
        // with no learning activity yet.
        var hasAnyLearningActivity = lastActivity != null || totalAttempts > 0 || interactions.Count > 0;

        var readinessBase = (averageMastery * 0.55) + ((1 - averageRisk) * 0.25) + (accuracyRate * 0.2);
        var recencyPenalty = lastActivity == null ? 0.15 : Math.Clamp((now - lastActivity.Value).TotalDays / 30.0, 0, 0.15);
        var examReadiness = hasAnyLearningActivity ? Clamp(readinessBase - recencyPenalty) : 0;

        var learningVelocity = await CalculateLearningVelocityAsync(userId, now, includeExams);

        return new LearningAnalyticsSummaryDto
        {
            TotalTopics = totalTopics,
            AverageMastery = averageMastery,
            AverageForgettingRisk = averageRisk,
            WeakTopicsCount = weakTopicsCount,
            TotalAttempts = totalAttempts,
            AccuracyRate = accuracyRate,
            ExamReadinessScore = examReadiness,
            LearningVelocity = learningVelocity,
            LastActivityAt = lastActivity
        };
    }

    public async Task<PerformanceTrendsDto> GetTrendsAsync(Guid userId, DateTime? from, DateTime? to, int bucketDays, bool includeExams)
    {
        var now = DateTime.UtcNow;
        var start = from ?? now.AddDays(-60);
        var end = to ?? now;
        var bucket = Math.Max(1, bucketDays);

        var attempts = await GetAttemptSnapshotsAsync(userId, includeExams, start, end);

        var points = new List<PerformanceTrendPointDto>();
        var cursor = start.Date;
        while (cursor < end)
        {
            var periodEnd = cursor.AddDays(bucket);
            var inBucket = attempts.Where(a => a.CreatedAt >= cursor && a.CreatedAt < periodEnd).ToList();
            var count = inBucket.Count;
            var averageScore = count > 0
                ? inBucket.Average(a => AdjustScoreForContext(a.Score, a.Difficulty, a.TimeSpentSeconds))
                : 0;

            points.Add(new PerformanceTrendPointDto
            {
                PeriodStart = cursor,
                PeriodEnd = periodEnd,
                AttemptCount = count,
                AverageScore = averageScore
            });

            cursor = periodEnd;
        }

        return new PerformanceTrendsDto
        {
            From = start,
            To = end,
            BucketDays = bucket,
            Points = points
        };
    }

    public async Task<CategoryBreakdownDto> GetCategoryBreakdownAsync(Guid userId, bool includeExams, string groupBy, IReadOnlyList<string> quizCategoryFilters)
    {
        var normalizedFilters = quizCategoryFilters
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var moduleQuery = context.Modules
            .AsNoTracking()
            .Where(m => m.OwnerUserId == userId)
            .Select(m => new { m.Id, CategoryLabel = NormalizeCategoryLabel(m.CategoryLabel) });

        var quizQuery = context.Quizzes
            .AsNoTracking()
            .Include(q => q.Note)
            .ThenInclude(n => n!.Module)
            .Where(q => q.Note != null && q.Note.Module!.OwnerUserId == userId);

        if (normalizedFilters.Count > 0)
        {
            quizQuery = quizQuery.Where(q => q.CategoryLabel != null && normalizedFilters.Contains(q.CategoryLabel));
        }

        var quizzes = await quizQuery
            .Select(q => new
            {
                q.Id,
                QuizCategory = NormalizeCategoryLabel(q.CategoryLabel),
                ModuleCategory = NormalizeCategoryLabel(q.Note!.Module!.CategoryLabel),
                ModuleId = q.Note!.Module!.Id
            })
            .ToListAsync();

        var attempts = await context.Attempts
            .AsNoTracking()
            .Include(a => a.Quiz)
            .ThenInclude(q => q!.Note)
            .ThenInclude(n => n!.Module)
            .Where(a => a.UserId == userId && a.Quiz != null && a.Quiz.Note != null)
            .ToListAsync();

        if (normalizedFilters.Count > 0)
        {
            attempts = attempts
                .Where(a => a.Quiz?.CategoryLabel != null && normalizedFilters.Contains(a.Quiz.CategoryLabel))
                .ToList();
        }

        var attemptGroups = attempts.Select(a => new
        {
            a.Score,
            a.CreatedAt,
            QuizCategory = NormalizeCategoryLabel(a.Quiz!.CategoryLabel),
            ModuleCategory = NormalizeCategoryLabel(a.Quiz!.Note!.Module!.CategoryLabel),
            ModuleId = a.Quiz!.Note!.Module!.Id
        }).ToList();

        string ResolveGroupLabel(string? quizCategory, string? moduleCategory)
        {
            return string.Equals(groupBy, "quizCategory", StringComparison.OrdinalIgnoreCase)
                ? (quizCategory ?? "Uncategorized")
                : (moduleCategory ?? "Uncategorized");
        }

        var moduleCounts = quizzes
            .GroupBy(q => ResolveGroupLabel(q.QuizCategory, q.ModuleCategory))
            .ToDictionary(g => g.Key, g => g.Select(x => x.ModuleId).Distinct().Count());

        var quizCounts = quizzes
            .GroupBy(q => ResolveGroupLabel(q.QuizCategory, q.ModuleCategory))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).Distinct().Count());

        var practiceByCategory = attemptGroups
            .GroupBy(a => ResolveGroupLabel(a.QuizCategory, a.ModuleCategory))
            .ToDictionary(g => g.Key, g => g.ToList());

        var categories = moduleCounts.Keys
            .Union(quizCounts.Keys)
            .Union(practiceByCategory.Keys)
            .Distinct()
            .OrderBy(label => label)
            .ToList();

        var rows = categories.Select(label =>
        {
            practiceByCategory.TryGetValue(label, out var practice);
            practice ??= [];

            return new CategoryBreakdownItemDto
            {
                CategoryLabel = label,
                ModuleCount = moduleCounts.GetValueOrDefault(label),
                QuizCount = quizCounts.GetValueOrDefault(label),
                PracticeAttemptCount = practice.Count,
                PracticeAverageScore = practice.Count > 0 ? practice.Average(a => a.Score) : 0,
                PracticeBestScore = practice.Count > 0 ? practice.Max(a => a.Score) : 0,
                LastPracticeAttemptAt = practice.Count > 0 ? practice.Max(a => a.CreatedAt) : null,
                ExamAttemptCount = 0,
                ExamAverageScore = 0,
                ExamBestScore = 0,
                LastExamAttemptAt = null
            };
        }).ToList();

        if (includeExams)
        {
            var exams = await context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Module)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            foreach (var group in exams.GroupBy(a => NormalizeCategoryLabel(a.Module?.CategoryLabel)))
            {
                var label = ResolveGroupLabel(null, group.Key);
                var rowIndex = rows.FindIndex(r => r.CategoryLabel == label);
                var existing = rowIndex >= 0 ? rows[rowIndex] : null;

                var updated = new CategoryBreakdownItemDto
                {
                    CategoryLabel = label,
                    ModuleCount = existing?.ModuleCount ?? 0,
                    QuizCount = existing?.QuizCount ?? 0,
                    PracticeAttemptCount = existing?.PracticeAttemptCount ?? 0,
                    PracticeAverageScore = existing?.PracticeAverageScore ?? 0,
                    PracticeBestScore = existing?.PracticeBestScore ?? 0,
                    LastPracticeAttemptAt = existing?.LastPracticeAttemptAt,
                    ExamAttemptCount = group.Count(),
                    ExamAverageScore = group.Any() ? group.Average(a => a.Score) : 0,
                    ExamBestScore = group.Any() ? group.Max(a => a.Score) : 0,
                    LastExamAttemptAt = group.Any() ? group.Max(a => a.CreatedAt) : null
                };

                if (rowIndex >= 0)
                {
                    rows[rowIndex] = updated;
                }
                else
                {
                    rows.Add(updated);
                }
            }

            rows = rows.OrderBy(r => r.CategoryLabel).ToList();
        }

        return new CategoryBreakdownDto { Items = rows };
    }

    public async Task<List<string>> GetQuizCategoriesAsync(Guid userId)
    {
        var categories = await context.Quizzes
            .AsNoTracking()
            .Include(q => q.Note)
            .ThenInclude(n => n!.Module)
            .Where(q => q.Note != null && q.Note.Module!.OwnerUserId == userId)
            .Select(q => q.CategoryLabel)
            .ToListAsync();

        return categories
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
    }

    public async Task<ExamAnalyticsSummaryDto> GetExamSummaryAsync(Guid userId)
    {
        var attempts = await context.ExamAttempts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return new ExamAnalyticsSummaryDto
        {
            TotalExamAttempts = attempts.Count,
            AverageScore = attempts.Count > 0 ? attempts.Average(a => a.Score) : 0,
            BestScore = attempts.Count > 0 ? attempts.Max(a => a.Score) : 0,
            LastAttemptAt = attempts.Count > 0 ? attempts.Max(a => a.CreatedAt) : null
        };
    }

    public async Task<CategoryBreakdownDto> GetExamCategoryBreakdownAsync(Guid userId)
    {
        var attempts = await context.ExamAttempts
            .AsNoTracking()
            .Include(a => a.Module)
            .Where(a => a.UserId == userId)
            .ToListAsync();

        var groups = attempts.GroupBy(a => NormalizeCategoryLabel(a.Module?.CategoryLabel));
        var rows = groups.Select(group => new CategoryBreakdownItemDto
        {
            CategoryLabel = group.Key,
            ModuleCount = group.Select(a => a.ModuleId).Distinct().Count(),
            QuizCount = 0,
            PracticeAttemptCount = 0,
            PracticeAverageScore = 0,
            PracticeBestScore = 0,
            LastPracticeAttemptAt = null,
            ExamAttemptCount = group.Count(),
            ExamAverageScore = group.Any() ? group.Average(a => a.Score) : 0,
            ExamBestScore = group.Any() ? group.Max(a => a.Score) : 0,
            LastExamAttemptAt = group.Any() ? group.Max(a => a.CreatedAt) : null
        }).OrderBy(r => r.CategoryLabel).ToList();

        return new CategoryBreakdownDto { Items = rows };
    }

    public async Task<TopicDistributionDto> GetTopicDistributionAsync(Guid userId, int maxTopics, int maxWeakTopics, bool includeExams)
    {
        var topicsLimit = Math.Max(1, maxTopics);
        var weakLimit = Math.Max(1, maxWeakTopics);

        var states = await context.UserKnowledgeStates
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync();

        var interactions = await GetInteractionsAsync(userId, includeExams);

        var topicStats = interactions
            .GroupBy(i => i.Topic)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    AttemptCount = g.Count(i => i.Type == LearningInteractionType.QuizAnswer),
                    CorrectCount = g.Count(i => i.IsCorrect == true),
                    IncorrectCount = g.Count(i => i.IsCorrect == false),
                    TotalCount = g.Count(i => i.IsCorrect.HasValue)
                });

        var maxIncorrect = topicStats.Values.Any() ? topicStats.Values.Max(s => s.IncorrectCount) : 0;

        var insights = states
            .Select(s =>
            {
                var stat = topicStats.GetValueOrDefault(s.Topic);
                var accuracy = stat == null || stat.TotalCount == 0 ? 0 : (double)stat.CorrectCount / stat.TotalCount;
                var dynamicRisk = decayService.CalculateForgettingRisk(s.MasteryScore, s.LastReviewedAt, DateTime.UtcNow);
                var incorrectFactor = maxIncorrect > 0 ? (stat?.IncorrectCount ?? 0) / (double)maxIncorrect : 0;
                var weaknessScore = Clamp(((1 - s.MasteryScore) * 0.5) + (dynamicRisk * 0.35) + (incorrectFactor * 0.15));

                return new TopicInsightDto
                {
                    Topic = s.Topic,
                    SourceNoteId = s.SourceNoteId,
                    MasteryScore = s.MasteryScore,
                    ForgettingRisk = dynamicRisk,
                    LastReviewedAt = s.LastReviewedAt,
                    NextReviewAt = s.NextReviewAt,
                    AttemptCount = stat?.AttemptCount ?? 0,
                    AccuracyRate = accuracy,
                    WeaknessScore = weaknessScore
                };
            })
            .OrderByDescending(t => t.MasteryScore)
            .Take(topicsLimit)
            .ToList();

        var weakest = insights
            .OrderByDescending(t => t.WeaknessScore)
            .Take(weakLimit)
            .ToList();

        return new TopicDistributionDto
        {
            Topics = insights,
            WeakestTopics = weakest
        };
    }

    public async Task<List<RetentionHeatmapPointDto>> GetRetentionHeatmapAsync(Guid userId, int maxTopics, bool includeExams)
    {
        var limit = Math.Max(1, maxTopics);

        var states = await context.UserKnowledgeStates
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(limit)
            .ToListAsync();

        return states
            .Select(s => new RetentionHeatmapPointDto
            {
                Topic = s.Topic,
                MasteryScore = s.MasteryScore,
                ForgettingRisk = decayService.CalculateForgettingRisk(s.MasteryScore, s.LastReviewedAt, DateTime.UtcNow)
            })
            .ToList();
    }

    public async Task<DecayForecastDto> GetDecayForecastAsync(Guid userId, int maxTopics, int days, int stepDays, bool includeExams)
    {
        var now = DateTime.UtcNow;
        var topicLimit = Math.Max(1, maxTopics);
        var horizon = Math.Max(1, days);
        var step = Math.Max(1, stepDays);

        var states = await context.UserKnowledgeStates
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ForgettingRisk)
            .Take(topicLimit)
            .ToListAsync();

        var topics = new List<DecayForecastTopicDto>();
        foreach (var state in states)
        {
            var points = new List<DecayForecastPointDto>();
            for (var day = 0; day <= horizon; day += step)
            {
                var date = now.Date.AddDays(day);
                var risk = decayService.CalculateForgettingRiskAt(state.MasteryScore, state.LastReviewedAt, now, date);
                points.Add(new DecayForecastPointDto
                {
                    Date = date,
                    ForgettingRisk = risk
                });
            }

            topics.Add(new DecayForecastTopicDto
            {
                Topic = state.Topic,
                Points = points
            });
        }

        return new DecayForecastDto
        {
            Days = horizon,
            StepDays = step,
            Topics = topics
        };
    }

    public async Task<List<MistakePatternSummaryDto>> GetMistakePatternsAsync(Guid userId, int maxItems, int maxTopics, bool includeExams)
    {
        var itemLimit = Math.Max(1, maxItems);
        var topicLimit = Math.Max(1, maxTopics);

        var patterns = await context.UserMistakePatterns
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync();

        if (patterns.Count == 0)
        {
            return [];
        }

        return patterns
            .GroupBy(p => p.Category)
            .Select(group => new MistakePatternSummaryDto
            {
                Category = group.Key,
                TotalCount = group.Sum(p => p.Count),
                TopTopics = group
                    .OrderByDescending(p => p.Count)
                    .ThenBy(p => p.Topic)
                    .Take(topicLimit)
                    .Select(p => new MistakePatternTopicDto
                    {
                        Topic = p.Topic,
                        Count = p.Count
                    })
                    .ToList()
            })
            .OrderByDescending(p => p.TotalCount)
            .ThenBy(p => p.Category)
            .Take(itemLimit)
            .ToList();
    }

    private async Task<double> CalculateLearningVelocityAsync(Guid userId, DateTime now, bool includeExams)
    {
        var trends = await GetTrendsAsync(userId, now.AddDays(-30), now, 7, includeExams);
        var points = trends.Points;
        if (points.Count < 2)
        {
            return 0;
        }

        var first = points.First().AverageScore;
        var last = points.Last().AverageScore;
        var scoreDelta = (last - first) / 100.0;

        var timeTrend = await CalculateTimeTrendAsync(userId, now, includeExams);
        var velocity = (scoreDelta * 0.7) + (timeTrend * 0.3);
        return Clamp(velocity);
    }

    private async Task<double> CalculateTimeTrendAsync(Guid userId, DateTime now, bool includeExams)
    {
        var from = now.AddDays(-30);
        var attempts = await GetAttemptSnapshotsAsync(userId, includeExams, from, null, true);

        if (attempts.Count < 2)
        {
            return 0;
        }

        var firstWindow = attempts.Where(a => a.CreatedAt < from.AddDays(15)).ToList();
        var secondWindow = attempts.Where(a => a.CreatedAt >= from.AddDays(15)).ToList();

        if (firstWindow.Count == 0 || secondWindow.Count == 0)
        {
            return 0;
        }

        var firstAvg = firstWindow.Average(a => a.TimeSpentSeconds ?? 0);
        var secondAvg = secondWindow.Average(a => a.TimeSpentSeconds ?? 0);

        if (firstAvg <= 0 || secondAvg <= 0)
        {
            return 0;
        }

        var improvement = (firstAvg - secondAvg) / firstAvg;
        return Clamp(improvement);
    }

    private static double AdjustScoreForContext(double score, string? difficulty, int? timeSpentSeconds)
    {
        var difficultyWeight = GetDifficultyWeight(difficulty);
        var timeFactor = GetTimeFactor(timeSpentSeconds);
        return score * difficultyWeight * timeFactor;
    }

    private static double CalculateWeightedAttemptAccuracy(List<AttemptSnapshot> attempts)
    {
        if (attempts.Count == 0)
        {
            return 0;
        }

        var weightedSum = 0.0;
        var weightTotal = 0.0;
        foreach (var attempt in attempts)
        {
            var weight = GetDifficultyWeight(attempt.Difficulty);
            weightedSum += attempt.Score * weight;
            weightTotal += weight;
        }

        if (weightTotal <= 0)
        {
            return 0;
        }

        return (weightedSum / weightTotal) / 100.0;
    }

    private static double GetDifficultyWeight(string? difficulty)
    {
        return difficulty?.Trim().ToLowerInvariant() switch
        {
            "beginner" => 0.9,
            "intermediate" => 1.0,
            "advanced" => 1.15,
            "professional" => 1.25,
            _ => 1.0
        };
    }

    private static double GetTimeFactor(int? timeSpentSeconds)
    {
        if (!timeSpentSeconds.HasValue || timeSpentSeconds <= 0)
        {
            return 1.0;
        }

        var seconds = timeSpentSeconds.Value;
        if (seconds <= 60)
        {
            return 1.05;
        }

        if (seconds >= 900)
        {
            return 0.85;
        }

        var normalized = (seconds - 60) / 840.0;
        return 1.05 - (normalized * 0.2);
    }

    private static double Clamp(double value)
    {
        return Math.Max(0, Math.Min(1, value));
    }


    private static string NormalizeCategoryLabel(string? label)
    {
        return string.IsNullOrWhiteSpace(label) ? "Uncategorized" : label.Trim();
    }

    private sealed record AttemptSnapshot(double Score, string? Difficulty, int? TimeSpentSeconds, DateTime CreatedAt);

    private async Task<List<AttemptSnapshot>> GetAttemptSnapshotsAsync(
        Guid userId,
        bool includeExams,
        DateTime? from = null,
        DateTime? to = null,
        bool requireTimeSpent = false)
    {
        var attemptQuery = context.Attempts.AsNoTracking().Where(a => a.UserId == userId);
        if (from.HasValue)
        {
            attemptQuery = attemptQuery.Where(a => a.CreatedAt >= from.Value);
        }
        if (to.HasValue)
        {
            attemptQuery = attemptQuery.Where(a => a.CreatedAt <= to.Value);
        }
        if (requireTimeSpent)
        {
            attemptQuery = attemptQuery.Where(a => a.TimeSpentSeconds.HasValue);
        }

        var attempts = await attemptQuery
            .Select(a => new AttemptSnapshot(a.Score, a.Difficulty, a.TimeSpentSeconds, a.CreatedAt))
            .ToListAsync();

        if (!includeExams)
        {
            return attempts;
        }

        var examQuery = context.ExamAttempts.AsNoTracking().Where(a => a.UserId == userId);
        if (from.HasValue)
        {
            examQuery = examQuery.Where(a => a.CreatedAt >= from.Value);
        }
        if (to.HasValue)
        {
            examQuery = examQuery.Where(a => a.CreatedAt <= to.Value);
        }
        if (requireTimeSpent)
        {
            examQuery = examQuery.Where(a => a.TimeSpentSeconds.HasValue);
        }

        var examAttempts = await examQuery
            .Select(a => new AttemptSnapshot(a.Score, a.Difficulty, a.TimeSpentSeconds, a.CreatedAt))
            .ToListAsync();

        attempts.AddRange(examAttempts);
        return attempts;
    }

    private async Task<List<LearningInteraction>> GetInteractionsAsync(Guid userId, bool includeExams)
    {
        var query = context.LearningInteractions.AsNoTracking().Where(i => i.UserId == userId);
        if (!includeExams)
        {
            query = query.Where(i => i.ExamAttemptId == null);
        }

        return await query.ToListAsync();
    }
}
