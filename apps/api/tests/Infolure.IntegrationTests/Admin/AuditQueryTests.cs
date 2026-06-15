using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Admin;

/// <summary>T047 / FR-021: consulta do histórico de auditoria com filtros e paginação.</summary>
public class AuditQueryTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Fact]
    public async Task Audit_filters_by_action_and_paginates()
    {
        var admin = _factory.AdminClient();

        // Gera uma ação auditável (deactivate sobre uma isca criada).
        var created = await admin.PostAsJsonAsync("/v1/admin/lures",
            new { slug = "t-audit-q-1", name = "Audit Q", lure_type = "jig", status = "published" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await admin.PutAsJsonAsync($"/v1/admin/lures/{id}/active", new { is_active = false });

        try
        {
            // filtro por ação
            var byAction = await admin.GetFromJsonAsync<JsonElement>("/v1/admin/audit?action=deactivate&per_page=50");
            var rows = byAction.GetProperty("data").EnumerateArray().ToList();
            Assert.All(rows, r => Assert.Equal("deactivate", r.GetProperty("action").GetString()));
            Assert.Contains(rows, r => r.GetProperty("entity_id").GetString() == id.ToString());

            // paginação
            var paged = await admin.GetFromJsonAsync<JsonElement>("/v1/admin/audit?per_page=1");
            Assert.Equal(1, paged.GetProperty("meta").GetProperty("per_page").GetInt32());
            Assert.True(paged.GetProperty("data").GetArrayLength() <= 1);
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Lures.IgnoreQueryFilters().Where(l => l.Id == id).ExecuteDeleteAsync();
        }
    }
}
