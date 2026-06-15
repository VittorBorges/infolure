using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infolure.IntegrationTests.Admin;

/// <summary>Helpers partilhados dos testes do backoffice (US-02): clientes admin/utilizador na BD.</summary>
internal static class AdminTestHelpers
{
    public const string AdminSub = "test-admin-sub-0001";

    public static HttpClient AdminClient(this AuthenticatedApiFactory f)
    {
        EnsureUser(f, AdminSub, "admin_tester", "admin");
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Add("X-Test-Sub", AdminSub);
        return c;
    }

    public static (HttpClient Client, string Sub, Guid UserId) UserClient(this AuthenticatedApiFactory f, string suffix)
    {
        var sub = $"test-u-{suffix}";
        var id = EnsureUser(f, sub, $"u_{suffix}", "user");
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Add("X-Test-Sub", sub);
        return (c, sub, id);
    }

    public static Guid EnsureUser(this AuthenticatedApiFactory f, string sub, string username, string role)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = db.Users.IgnoreQueryFilters()
            .FirstOrDefault(u => u.AuthProviders.Any(p => p.ProviderUid == sub));
        if (existing is not null) return existing.Id;

        var u = new User { Id = Guid.NewGuid(), Username = username, Role = role };
        u.AuthProviders.Add(new UserAuthProvider { Id = Guid.NewGuid(), Provider = "test", ProviderUid = sub });
        db.Users.Add(u);
        db.SaveChanges();
        return u.Id;
    }

    public static async Task HardDeleteUserAsync(this AuthenticatedApiFactory f, Guid userId)
    {
        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Users.IgnoreQueryFilters().Where(u => u.Id == userId).ExecuteDeleteAsync();
    }
}
