using System.Text.Json;
using Infolure.Api.Features.Catalog;
using Infolure.Api.Infrastructure.Auth;
using Infolure.Api.Infrastructure.Errors;
using Infolure.Api.Infrastructure.Persistence;
using Infolure.Api.Infrastructure.RateLimiting;
using Infolure.Api.Infrastructure.Search;
using Infolure.Api.Infrastructure.Seed;
using Infolure.Api.Observability;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Json;
using Typesense;

var builder = WebApplication.CreateBuilder(args);

// T008 — Serilog: logging estruturado JSON (Princípio II)
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()));

// Infraestrutura (Foundational)
builder.Services.AddPersistence(builder.Configuration);            // T009
builder.Services.AddSupabaseAuth(builder.Configuration);           // T011
builder.Services.AddInfolureRateLimiting(builder.Configuration);   // T012
builder.Services.AddTypesenseSearch(builder.Configuration);        // T013

// T014 — erros como ProblemDetails
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Serviços de feature — Catálogo (US-01/02/03)
builder.Services.AddScoped<LureListService>();
builder.Services.AddScoped<SuggestService>();
builder.Services.AddScoped<LureDetailService>();
builder.Services.AddScoped<LureIndexer>();

// Controllers (vertical slices nas fases de user story) + OpenAPI (T016)
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Infolure API — Catalog", Version = "v1" }));

var app = builder.Build();

// Pipeline
app.UseExceptionHandler();          // T014
app.UseCorrelationId();             // T008
app.UseSerilogRequestLogging();     // T008 — loga cada requisição (fronteira de rede) com latência

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();                  // T015 — HSTS
}
app.UseHttpsRedirection();          // T015 — HTTPS forçado

app.UseRateLimiter();               // T012
app.UseAuthentication();            // T011
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

// Tarefas de arranque (dev): aplicar migrations, seed e coleção Typesense.
// Guardadas por flag para o build/arranque não exigir os serviços ativos.
if (app.Configuration.GetValue("RunStartupTasks", false))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await Seeder.SeedAsync(db);
        var typesense = scope.ServiceProvider.GetRequiredService<ITypesenseClient>();
        await typesense.EnsureLuresCollectionAsync();
        var indexed = await scope.ServiceProvider.GetRequiredService<LureIndexer>().ReindexAllAsync();
        app.Logger.LogInformation("Typesense: {Count} iscas indexadas.", indexed);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Falha nas tarefas de arranque (serviços disponíveis?).");
    }
}

app.Run();

public partial class Program; // expõe Program para testes de integração (WebApplicationFactory)
