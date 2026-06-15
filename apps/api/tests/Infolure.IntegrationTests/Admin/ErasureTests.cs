using System.Net;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Admin;

/// <summary>T036b / FR-012a: eliminação RGPD efetiva (admin) — anonimiza PII e remove vínculos de
/// auth de forma irreversível, distinta do soft-delete.</summary>
public class ErasureTests(AuthenticatedApiFactory factory) : IClassFixture<AuthenticatedApiFactory>
{
    private readonly AuthenticatedApiFactory _factory = factory;

    [Fact]
    public async Task Admin_erase_anonymizes_personal_data()
    {
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u = new User { Id = Guid.NewGuid(), Username = "erase_target", Email = "erase@x.pt", DisplayName = "Nome Real", Role = "user" };
            u.AuthProviders.Add(new UserAuthProvider { Id = Guid.NewGuid(), Provider = "test", ProviderUid = "test-erase-sub" });
            db.Users.Add(u);
            await db.SaveChangesAsync();
            userId = u.Id;
        }

        try
        {
            var res = await _factory.AdminClient().PostAsync($"/v1/admin/users/{userId}/erase", null);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u = await db.Users.IgnoreQueryFilters().FirstAsync(x => x.Id == userId);
            Assert.Null(u.Email);            // PII removida
            Assert.Null(u.DisplayName);
            Assert.NotNull(u.DeletedAt);     // marcado eliminado
            Assert.False(u.IsActive);
            Assert.False(await db.UserAuthProviders.IgnoreQueryFilters().AnyAsync(p => p.UserId == userId)); // acesso revogado

            // auditado como ação sobre dados pessoais
            Assert.True(await db.AdminAuditLog.AnyAsync(a => a.EntityType == "users" && a.EntityId == userId.ToString() && a.IsPersonalData));
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Users.IgnoreQueryFilters().Where(u => u.Id == userId).ExecuteDeleteAsync();
        }
    }
}
