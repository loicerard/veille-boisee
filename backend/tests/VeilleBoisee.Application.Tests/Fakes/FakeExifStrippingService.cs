using VeilleBoisee.Application.Abstractions;

namespace VeilleBoisee.Application.Tests.Fakes;

internal sealed class FakeExifStrippingService : IExifStrippingService
{
    public async Task<byte[]> StripExifAsync(Stream photoStream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await photoStream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}
