using System.Net;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Lifecycle;

/// <summary>
/// T015 / US-01: Remove converte-se em soft-delete (interceptor); o restauro repõe a visibilidade
/// preservando o IsActive anterior (FR-004); e a origem (Source) reflete a proveniência.
/// </summary>
public class RestoreAndSourceTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory = factory;

    private async Task<HttpStatusCode> DetailStatus(string slug)
        => (await _factory.CreateClient().GetAsync($"/v1/lures/{slug}")).StatusCode;

    [Fact]
    public async Task Remove_softdeletes_and_restore_brings_back_preserving_active()
    {
        var (brandId, lureId) = await _factory.CreateLureAsync("rest1");
        const string slug = "t-lure-rest1";
        try
        {
            Assert.Equal(HttpStatusCode.OK, await DetailStatus(slug));

            // Remove → o interceptor converte em soft-delete (não apaga a linha)
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var lure = await db.Lures.FirstAsync(l => l.Id == lureId);
                db.Lures.Remove(lure);
                await db.SaveChangesAsync();
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var lure = await db.Lures.IgnoreQueryFilters().FirstAsync(l => l.Id == lureId);
                Assert.NotNull(lure.DeletedAt);   // linha preservada, marcada eliminada
                Assert.True(lure.IsActive);        // atividade não foi tocada pelo delete
            }
            Assert.Equal(HttpStatusCode.NotFound, await DetailStatus(slug));

            // Restaurar → visível de novo, IsActive preservado
            await _factory.MutateAsync<Lure>(lureId, l => l.DeletedAt = null);
            Assert.Equal(HttpStatusCode.OK, await DetailStatus(slug));
        }
        finally
        {
            await _factory.HardDeleteAsync(brandId, lureId);
        }
    }

    [Fact]
    public async Task Source_reflects_provenance()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed/automação → 'automation' (backfill T010 / seeder T019)
        var seeded = await db.Lures.FirstAsync(l => l.Slug == "isca-001");
        Assert.Equal("automation", seeded.Source);

        // Criada diretamente (default) → 'manual'
        var (brandId, lureId) = await _factory.CreateLureAsync("src1");
        try
        {
            var created = await db.Lures.IgnoreQueryFilters().FirstAsync(l => l.Id == lureId);
            Assert.Equal("manual", created.Source);
        }
        finally
        {
            await _factory.HardDeleteAsync(brandId, lureId);
        }
    }
}
