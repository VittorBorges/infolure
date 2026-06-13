using Typesense;
using Typesense.Setup;

namespace Infolure.Api.Infrastructure.Search;

/// <summary>
/// Registo do cliente Typesense e bootstrap idempotente da coleção "lures"
/// (busca facetada + autocomplete — research.md §4). Sincronização write-through
/// fica nos serviços de catálogo (fase de user stories).
/// </summary>
public static class TypesenseExtensions
{
    public const string CollectionName = "lures";

    public static IServiceCollection AddTypesenseSearch(this IServiceCollection services, IConfiguration config)
    {
        var host = config["Typesense:Host"] ?? "localhost";
        var port = config["Typesense:Port"] ?? "8108";
        var protocol = config["Typesense:Protocol"] ?? "http";
        var apiKey = config["Typesense:ApiKey"] ?? "devkey";

        services.AddTypesenseClient(c =>
        {
            c.ApiKey = apiKey;
            c.Nodes = [new Node(host, port, protocol)];
        });

        return services;
    }

    /// <summary>Cria a coleção "lures" se ainda não existir. Idempotente.</summary>
    public static async Task EnsureLuresCollectionAsync(this ITypesenseClient client, CancellationToken ct = default)
    {
        try
        {
            await client.RetrieveCollection(CollectionName);
            return; // já existe
        }
        catch
        {
            // não existe — cria abaixo
        }

        var schema = new Schema(
            CollectionName,
            [
                new Field("id", FieldType.String, false),
                new Field("slug", FieldType.String, false),
                new Field("model_ref", FieldType.String, false, true),
                new Field("name_pt", FieldType.String, false),
                new Field("name_en", FieldType.String, false, true),
                new Field("name_es", FieldType.String, false, true),
                new Field("brand_name", FieldType.String, true),
                new Field("lure_type", FieldType.String, true),
                new Field("water_type", FieldType.String, true),
                new Field("weight_g", FieldType.Float, false, true),
                new Field("depth_min_m", FieldType.Float, false, true),
                new Field("depth_max_m", FieldType.Float, false, true),
                new Field("target_species", FieldType.StringArray, true),
                new Field("price_6m_avg_eur", FieldType.Float, false, true),
                new Field("primary_image_url", FieldType.String, false, true),
                new Field("status", FieldType.String, false),
                new Field("popularity_score", FieldType.Int32, false),
                new Field("favorites_count", FieldType.Int32, false),
                new Field("created_at", FieldType.Int64, false),
            ],
            "popularity_score");

        await client.CreateCollection(schema);
    }
}
