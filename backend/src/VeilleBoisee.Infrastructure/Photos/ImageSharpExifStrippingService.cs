using SixLabors.ImageSharp;
using VeilleBoisee.Application.Abstractions;

namespace VeilleBoisee.Infrastructure.Photos;

internal sealed class ImageSharpExifStrippingService : IExifStrippingService
{
    public async Task<byte[]> StripExifAsync(Stream photoStream, CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync(photoStream, cancellationToken);

        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        using var output = new MemoryStream();
        await image.SaveAsync(output, image.Metadata.DecodedImageFormat!, cancellationToken);
        return output.ToArray();
    }
}
