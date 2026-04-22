using VeilleBoisee.Domain.Common;
using VeilleBoisee.Domain.Enums;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Domain.Entities;

public sealed class Report
{
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 2000;

    private Report() { }

    public Guid Id { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string CommuneInsee { get; private set; } = string.Empty;
    public string CommuneName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string EncryptedContactEmail { get; private set; } = string.Empty;
    public ReportStatus Status { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    // Enrichissement géographique — null = non encore enrichi
    public string? ParcelleSection { get; private set; }
    public string? ParcelleNumero { get; private set; }
    public bool? IsInForest { get; private set; }
    public bool? IsInNatura2000Zone { get; private set; }

    public byte[]? PhotoData { get; private set; }
    public string? PhotoMimeType { get; private set; }

    public static Report Create(
        Coordinates location,
        CodeInsee communeInsee,
        string communeName,
        string description,
        string encryptedContactEmail,
        byte[]? photoData = null,
        string? photoMimeType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(communeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedContactEmail);

        return new Report
        {
            Id = Guid.NewGuid(),
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            CommuneInsee = communeInsee.Value,
            CommuneName = communeName,
            Description = description,
            EncryptedContactEmail = encryptedContactEmail,
            Status = ReportStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow,
            PhotoData = photoData,
            PhotoMimeType = photoMimeType
        };
    }

    public void Enrich(string? parcelleSection, string? parcelleNumero, bool isInForest, bool isInNatura2000Zone)
    {
        ParcelleSection = parcelleSection;
        ParcelleNumero = parcelleNumero;
        IsInForest = isInForest;
        IsInNatura2000Zone = isInNatura2000Zone;
    }

    public Result<ReportStatus, ReportStatusTransitionError> UpdateStatus(ReportStatus next)
    {
        var valid = (Status, next) is
            (ReportStatus.Pending, ReportStatus.Routed) or
            (ReportStatus.Routed, ReportStatus.Acknowledged) or
            (ReportStatus.Acknowledged, ReportStatus.Closed);

        if (!valid)
            return ReportStatusTransitionError.InvalidTransition;

        Status = next;
        return Status;
    }
}
