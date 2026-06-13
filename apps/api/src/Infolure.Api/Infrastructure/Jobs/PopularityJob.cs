using Infolure.Api.Infrastructure.Search;

namespace Infolure.Api.Infrastructure.Jobs;

/// <summary>
/// Job noturno (T082): recalcula popularity_score (favoritos + inventário) e reindexa o catálogo
/// no Typesense. Intervalo configurável; desativado por omissão (ativar com Jobs:PopularityEnabled=true).
/// </summary>
public class PopularityJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<PopularityJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Jobs:PopularityEnabled", false))
        {
            logger.LogInformation("PopularityJob desativado (Jobs:PopularityEnabled=false).");
            return;
        }

        var hours = config.GetValue("Jobs:PopularityIntervalHours", 24);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(hours));

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var indexer = scope.ServiceProvider.GetRequiredService<LureIndexer>();
                var count = await indexer.ReindexAllAsync(stoppingToken);
                logger.LogInformation("PopularityJob: {Count} iscas reindexadas.", count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PopularityJob falhou nesta iteração.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
