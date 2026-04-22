namespace VeilleBoisee.Application.Abstractions;

public interface IExifStrippingService
{
    Task<byte[]> StripExifAsync(Stream photoStream, CancellationToken cancellationToken);
}
