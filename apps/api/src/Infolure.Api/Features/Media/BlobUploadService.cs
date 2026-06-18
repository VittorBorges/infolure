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

    public async Task<UploadResult> UploadAsync(Stream content, string contentType, long length, string fileName, CancellationToken ct)
    {
        if (!IsConfigured) return new UploadResult(Outcome.NotConfigured, null);
        if (!AllowedContentTypes.Contains(contentType)) return new UploadResult(Outcome.UnsupportedType, null);
        if (length > MaxBytes) return new UploadResult(Outcome.TooLarge, null);

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
