using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Infolure.Api.Infrastructure.Auth;

/// <summary>
/// Valida os JWTs emitidos pelo Supabase Auth (broker OIDC: Google + Microsoft MSA + email/senha).
/// O backend não emite tokens; apenas valida assinatura/issuer/audience via JWKS do Supabase.
/// Ver research.md §3.
/// </summary>
public static class AuthExtensions
{
    public const string AdminPolicy = "admin";
    public const string UserPolicy = "user";

    public static IServiceCollection AddSupabaseAuth(this IServiceCollection services, IConfiguration config)
    {
        var authority = config["Supabase:Authority"];   // ex.: https://<project>.supabase.co/auth/v1
        var audience = config["Supabase:Audience"] ?? "authenticated";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrEmpty(authority),
                    ValidIssuer = authority,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // As chaves de assinatura são obtidas automaticamente do JWKS em {authority}/.well-known/jwks.json
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(UserPolicy, p => p.RequireAuthenticatedUser())
            .AddPolicy(AdminPolicy, p => p.RequireAuthenticatedUser().RequireClaim("role", "admin"));

        return services;
    }
}
