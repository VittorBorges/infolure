using System.Net;
using Infolure.Api.Infrastructure.Persistence.Entities;

namespace Infolure.IntegrationTests.Lifecycle;

/// <summary>
/// T013 / US-01: registos inativos ou eliminados saem das superfícies públicas (detalhe);
/// e independência de estado (FR-006): atividade, eliminação e estado editorial são ortogonais.
/// </summary>
public class VisibilityTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory = factory;

    private async Task<HttpStatusCode> DetailStatus(string slug)
        => (await _factory.CreateClient().GetAsync($"/v1/lures/{slug}")).StatusCode;

    [Fact]
    public async Task Inactive_or_deleted_lure_is_hidden_then_visible_again()
    {
        var (brandId, lureId) = await _factory.CreateLureAsync("vis1");
        const string slug = "t-lure-vis1";
        try
        {
            Assert.Equal(HttpStatusCode.OK, await DetailStatus(slug));

            // inativa → oculta
            await _factory.MutateAsync<Lure>(lureId, l => l.IsActive = false);
            Assert.Equal(HttpStatusCode.NotFound, await DetailStatus(slug));

            // reativa → visível
            await _factory.MutateAsync<Lure>(lureId, l => l.IsActive = true);
            Assert.Equal(HttpStatusCode.OK, await DetailStatus(slug));

            // soft-delete → oculta (global query filter)
            await _factory.MutateAsync<Lure>(lureId, l => l.DeletedAt = DateTimeOffset.UtcNow);
            Assert.Equal(HttpStatusCode.NotFound, await DetailStatus(slug));
        }
        finally
        {
            await _factory.HardDeleteAsync(brandId, lureId);
        }
    }

    [Fact]
    public async Task State_dimensions_are_independent_FR006()
    {
        var (brandId, lureId) = await _factory.CreateLureAsync("vis2");
        const string slug = "t-lure-vis2";
        try
        {
            Assert.Equal(HttpStatusCode.OK, await DetailStatus(slug));

            // estado editorial 'archived' (ativo) → oculto pela publicação, não pela atividade
            await _factory.MutateAsync<Lure>(lureId, l => l.Status = "archived");
            Assert.Equal(HttpStatusCode.NotFound, await DetailStatus(slug));

            // volta a publicado mas inativo → oculto pela atividade, não pela publicação
            await _factory.MutateAsync<Lure>(lureId, l => { l.Status = "published"; l.IsActive = false; });
            Assert.Equal(HttpStatusCode.NotFound, await DetailStatus(slug));

            // publicado + ativo → visível
            await _factory.MutateAsync<Lure>(lureId, l => l.IsActive = true);
            Assert.Equal(HttpStatusCode.OK, await DetailStatus(slug));
        }
        finally
        {
            await _factory.HardDeleteAsync(brandId, lureId);
        }
    }
}
