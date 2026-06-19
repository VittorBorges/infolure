using System.Net.Http.Json;
using System.Text.Json;
using Infolure.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Admin;

/// <summary>
/// T025 / FR-020/FR-020a/SC-007: ações de escrita do painel geram auditoria automática; operações
/// sobre dados pessoais incluem instantâneo dos campos alterados (antes→depois).
/// </summary>
public class AuditWriteTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Fact]
    public async Task Deactivating_lure_writes_audit_without_snapshot()
    {
        var admin = _factory.AdminClient();
        const string slug = "t-audit-lure-1";
        var created = await admin.PostAsJsonAsync("/v1/admin/lures",
            new { slug, name = "Audit Lure", lure_type = "jig", status = "published", configurations = new[] { new { label = "STD", weight_g = 10 } } });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        try
        {
            await admin.PutAsJsonAsync($"/v1/admin/lures/{id}/active", new { is_active = false });

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entry = await db.AdminAuditLog
                .Where(a => a.EntityType == "lures" && a.EntityId == id.ToString())
                .OrderByDescending(a => a.CreatedAt).FirstAsync();

            Assert.Equal("deactivate", entry.Action);
            Assert.False(entry.IsPersonalData);
            Assert.Null(entry.Changes);
            Assert.NotNull(entry.ActorUserId); // actor = admin resolvido
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Lures.IgnoreQueryFilters().Where(l => l.Id == id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Personal_data_change_writes_audit_with_snapshot()
    {
        var admin = _factory.AdminClient();
        var userId = _factory.EnsureUser("test-audit-pd", "audit_pd", "user");
        try
        {
            await admin.PutAsJsonAsync($"/v1/admin/users/{userId}/active", new { is_active = false });

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entry = await db.AdminAuditLog
                .Where(a => a.EntityType == "users" && a.EntityId == userId.ToString())
                .OrderByDescending(a => a.CreatedAt).FirstAsync();

            Assert.Equal("deactivate", entry.Action);
            Assert.True(entry.IsPersonalData);
            Assert.NotNull(entry.Changes);
            Assert.Contains("IsActive", entry.Changes); // instantâneo antes→depois
        }
        finally
        {
            await _factory.HardDeleteUserAsync(userId);
        }
    }
}
