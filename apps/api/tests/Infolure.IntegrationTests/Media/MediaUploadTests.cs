using Infolure.Api.Features.Media;

namespace Infolure.IntegrationTests.Media;

/// <summary>
/// Feature 006 (US5/FR-012) — valida o limite de upload de fotos. Prova que o limite é 5 MB
/// (aceita > 1 MB, corrigindo o bug) e recusa > 5 MB / tipos não suportados, sem depender do Azure.
/// </summary>
public class MediaUploadTests
{
    [Theory]
    [InlineData("image/jpeg", 2 * 1024 * 1024)]   // 2 MB — antes falhava (limite 1 MB), agora ok
    [InlineData("image/png", 5 * 1024 * 1024)]    // 5 MB exatos — no limite, ok
    [InlineData("image/webp", 1024)]              // pequeno, ok
    public void Accepts_images_up_to_5mb(string contentType, long length)
    {
        Assert.Null(BlobUploadService.Validate(contentType, length));
    }

    [Fact]
    public void Rejects_over_5mb()
    {
        Assert.Equal(BlobUploadService.Outcome.TooLarge, BlobUploadService.Validate("image/jpeg", 6 * 1024 * 1024));
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("image/gif")]
    public void Rejects_unsupported_types(string contentType)
    {
        Assert.Equal(BlobUploadService.Outcome.UnsupportedType, BlobUploadService.Validate(contentType, 1024));
    }
}
