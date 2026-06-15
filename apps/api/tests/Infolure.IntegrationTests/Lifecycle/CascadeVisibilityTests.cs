using System.Net;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Lifecycle;

/// <summary>
/// T014 / US-01: visibilidade em cascata pelo PAI verdadeiro (marca→isca, FR-003a) e
/// não-cascata da relação FRACA (espécie-alvo, FR-003b).
/// </summary>
public class CascadeVisibilityTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory = factory;

    private async Task<HttpStatusCode> DetailStatus(string slug)
        => (await _factory.CreateClient().GetAsync($"/v1/lures/{slug}")).StatusCode;

    [Fact]
    public async Task Inactive_or_deleted_brand_hides_its_lures()
    {
        var (brandId, lureId) = await _factory.CreateLureAsync("casc1");
        const string slug = "t-lure-casc1";
        try
        {
            Assert.Equal(HttpStatusCode.OK, await DetailStatus(slug));

            // marca inativa → isca oculta (cascata de visibilidade), sem mudar o estado da isca
            await _factory.MutateAsync<Brand>(brandId, b => b.IsActive = false);
            Assert.Equal(HttpStatusCode.NotFound, await DetailStatus(slug));

            await _factory.MutateAsync<Brand>(brandId, b => b.IsActive = true);
            Assert.Equal(HttpStatusCode.OK, await DetailStatus(slug));

            // marca soft-deleted → isca oculta
            await _factory.MutateAsync<Brand>(brandId, b => b.DeletedAt = DateTimeOffset.UtcNow);
            Assert.Equal(HttpStatusCode.NotFound, await DetailStatus(slug));

            // restaurar a marca → isca visível de novo
            await _factory.MutateAsync<Brand>(brandId, b => b.DeletedAt = null);
            Assert.Equal(HttpStatusCode.OK, await DetailStatus(slug));

            // a isca em si nunca foi alterada
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var lure = await db.Lures.IgnoreQueryFilters().FirstAsync(l => l.Id == lureId);
            Assert.True(lure.IsActive);
            Assert.Null(lure.DeletedAt);
        }
        finally
        {
            await _factory.HardDeleteAsync(brandId, lureId);
        }
    }

    [Fact]
    public async Task Inactive_species_does_not_hide_lure_only_drops_from_targets()
    {
        Guid speciesId;
        string speciesSlug;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sp = await db.Species.FirstAsync(s => s.Slug == "especie-01");
            speciesId = sp.Id; speciesSlug = sp.Slug;
        }

        var (brandId, lureId) = await _factory.CreateLureAsync("casc2", targetSpeciesId: speciesId);
        const string slug = "t-lure-casc2";
        try
        {
            var before = await _factory.CreateClient().GetStringAsync($"/v1/lures/{slug}");
            Assert.Contains(speciesSlug, before); // espécie presente nos alvos

            // espécie inativa: a isca CONTINUA visível e a espécie sai dos alvos
            await _factory.MutateAsync<Species>(speciesId, s => s.IsActive = false);
            try
            {
                var res = await _factory.CreateClient().GetAsync($"/v1/lures/{slug}");
                Assert.Equal(HttpStatusCode.OK, res.StatusCode); // não-cascata (FR-003b)
                var after = await res.Content.ReadAsStringAsync();
                Assert.DoesNotContain(speciesSlug, after);
            }
            finally
            {
                await _factory.MutateAsync<Species>(speciesId, s => s.IsActive = true);
            }
        }
        finally
        {
            await _factory.HardDeleteAsync(brandId, lureId);
        }
    }
}
