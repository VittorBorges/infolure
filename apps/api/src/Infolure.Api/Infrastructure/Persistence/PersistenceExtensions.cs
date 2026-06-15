using Infolure.Api.Infrastructure.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=infolure;Username=postgres;Password=dev";

        // Feature 002: interceptor de soft-delete/auditável (T006).
        services.AddSingleton<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention()
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        return services;
    }
}
