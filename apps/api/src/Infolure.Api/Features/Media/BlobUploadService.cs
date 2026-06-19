using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Infolure.Api.Features.Media;

/// <summary>
/// Feature 005 — upload de fotos (foto de cor) para Azure Blob Storage. Valida tipo e tamanho,
/// devolve a URL pública. Connection string em Azure:Blob:ConnectionString (user-secrets/env);
/// em dev pode apontar para Azurite. Sem configuração, IsConfigured == false.
/// </summary>
public class BlobUploadService(IConfiguration config, ILogger<BlobUploadService> log)
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
    private readonly string? _conn = config["Azure:Blob:ConnectionString"];
    private readonly string _container = config["Azure:Blob:Container"] ?? "lure-photos";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_conn);

    public enum Outcome { Ok, NotConfigured, UnsupportedType, TooLarge }

    public record UploadResult(Outcome Outcome, string? Url);

    /// <summary>
    /// Validação pura de tipo/tamanho (Feature 006/FR-012, testável sem Azure). Limite = 5 MB:
    /// aceita > 1 MB (corrige o bug) e recusa > 5 MB. <c>null</c> = válido.
    /// </summary>
    public static Outcome? Validate(string contentType, long length)
    {
        if (!AllowedContentTypes.Contains(contentType)) return Outcome.UnsupportedType;
        if (length > MaxBytes) return Outcome.TooLarge;
        return null;
    }

    public async Task<UploadResult> UploadAsync(Stream content, string contentType, long length, string fileName, CancellationToken ct)
    {
        if (!IsConfigured) return new UploadResult(Outcome.NotConfigured, null);
        if (Validate(contentType, length) is { } bad) return new UploadResult(bad, null);

        var container = new BlobContainerClient(_conn, _container);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var ext = Path.GetExtension(fileName);
        var blobName = $"{Guid.NewGuid():N}{ext}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, ct);

        log.LogInformation("Foto carregada {Blob} ({Bytes} bytes)", blobName, length);
        return new UploadResult(Outcome.Ok, blob.Uri.ToString());
    }
}
