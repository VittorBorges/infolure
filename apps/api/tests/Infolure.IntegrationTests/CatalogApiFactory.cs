using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Infolure.IntegrationTests;

/// <summary>
/// Boota a API real apontando para os serviços do docker-compose (Postgres :5433, Typesense :8108).
/// Pré-requisito: `docker compose up -d` com a seed/índice já aplicados (RunStartupTasks no app dev).
/// NOTA: estes testes assumem os serviços locais ativos (deviação pragmática vs. Testcontainers — T021).
///
/// As variáveis de ambiente são definidas no construtor (não via ConfigureAppConfiguration) porque
/// AddPersistence/AddTypesenseSearch leem a configuração avidamente no registo dos serviços —
/// antes de qualquer ConfigureAppConfiguration do host de teste correr. Env vars são lidas já no
/// CreateBuilder, garantindo que o override (ex.: Postgres na 5433) é aplicado a tempo.
/// </summary>
public class CatalogApiFactory : WebApplicationFactory<Program>
{
    public CatalogApiFactory()
    {
        Environment.SetEnvironmentVariable("RunStartupTasks", "false");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Port=5433;Database=infolure;Username=postgres;Password=dev");
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", "localhost:6379");
        Environment.SetEnvironmentVariable("Typesense__Host", "localhost");
        Environment.SetEnvironmentVariable("Typesense__Port", "8108");
        Environment.SetEnvironmentVariable("Typesense__Protocol", "http");
        Environment.SetEnvironmentVariable("Typesense__ApiKey", "devkey");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
