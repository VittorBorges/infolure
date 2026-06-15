using Infolure.Api.Infrastructure.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Infolure.Api.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=infolure;Username=postgres;Password=dev";

        // Feature 002: interceptors (T006 soft-delete, T026 auditoria) + contexto de ação admin.
        services.AddSingleton<AuditSaveChangesInterceptor>();
        services.AddSingleton<AdminAuditInterceptor>();
        services.AddScoped<IAdminActionContext, AdminActionContext>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention()
                   // Ordem importa: soft-delete converte Deleted→Modified ANTES da auditoria observar o estado.
                   .AddInterceptors(
                       sp.GetRequiredService<AuditSaveChangesInterceptor>(),
                       sp.GetRequiredService<AdminAuditInterceptor>()));

        return services;
    }
}
