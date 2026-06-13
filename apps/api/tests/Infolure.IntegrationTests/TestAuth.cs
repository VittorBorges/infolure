using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infolure.IntegrationTests;

/// <summary>
/// Esquema de autenticação de teste: autentica toda requisição com um `sub` fixo,
/// permitindo exercitar endpoints [Authorize] sem um JWT real do Supabase.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "Test";
    public const string Sub = "test-fav-sub-0001";
    public const string SubHeader = "X-Test-Sub";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Permite que cada classe de teste use um `sub` distinto (evita corrida entre classes paralelas).
        var sub = Request.Headers.TryGetValue(SubHeader, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : Sub;
        var claims = new[] { new Claim("sub", sub), new Claim(ClaimTypes.Name, "tester") };
        var identity = new ClaimsIdentity(claims, Scheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>Factory que torna todas as requisições autenticadas (sub = TestAuthHandler.Sub).</summary>
public class AuthenticatedApiFactory : CatalogApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
        });
    }
}
