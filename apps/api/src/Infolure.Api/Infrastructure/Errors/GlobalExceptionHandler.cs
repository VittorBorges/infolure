using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Infolure.Api.Infrastructure.Errors;

/// <summary>
/// Converte exceções não tratadas em ProblemDetails RFC 7807 — mensagem amigável
/// para o cliente (Princípio V), detalhes técnicos só no log (Princípio II).
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[Observability.CorrelationIdMiddleware.HeaderName]?.ToString();

        logger.LogError(exception,
            "Unhandled exception. CorrelationId={CorrelationId} Path={Path}",
            correlationId, httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ocorreu um erro inesperado.",
            Detail = "Tente novamente. Se persistir, contacte o suporte com o identificador abaixo.",
            Instance = httpContext.Request.Path,
        };
        if (!string.IsNullOrEmpty(correlationId))
            problem.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = problem.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
