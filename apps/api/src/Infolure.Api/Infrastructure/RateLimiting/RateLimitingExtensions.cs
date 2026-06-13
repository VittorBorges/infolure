using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace Infolure.Api.Infrastructure.RateLimiting;

/// <summary>
/// Rate limiting nativo do ASP.NET Core (NFR: 100 req/min por IP anônimo, 300 req/min por utilizador).
/// O Redis é registado como store distribuído/consistente entre instâncias (research.md §5);
/// o particionamento usa o subject do JWT quando autenticado, senão o IP.
/// </summary>
public static class RateLimitingExtensions
{
    public const string PolicyName = "per-client";

    public static IServiceCollection AddInfolureRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var redisConn = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;
                var authenticated = !string.IsNullOrEmpty(userId);
                var partitionKey = authenticated
                    ? $"user:{userId}"
                    : $"ip:{httpContext.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authenticated ? 300 : 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }
}
