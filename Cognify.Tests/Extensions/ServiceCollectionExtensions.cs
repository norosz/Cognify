using Cognify.Server.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace Cognify.Tests.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSqliteTestDatabase<TContext>(this IServiceCollection services) where TContext : DbContext
    {
        // Aggressively remove all DbContext-related services
        var dbContextType = typeof(TContext);
        var servicesToRemove = services.Where(d =>
            d.ServiceType == dbContextType ||
            d.ServiceType == typeof(DbContextOptions<TContext>) ||
            (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(dbContextType))
        ).ToList();

        foreach (var descriptor in servicesToRemove)
        {
            services.Remove(descriptor);
        }

        // Create and open SQLite connection
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        services.AddSingleton<DbConnection>(connection);

        services.AddDbContext<TContext>((sp, opt) =>
        {
            var conn = sp.GetRequiredService<DbConnection>();
            opt.UseSqlite(conn);
        });

        // Ensure database is created
        var sp = services.BuildServiceProvider();
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            db.Database.EnsureCreated();
        }
    }
}
