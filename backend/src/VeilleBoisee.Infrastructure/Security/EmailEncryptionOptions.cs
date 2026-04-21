namespace VeilleBoisee.Infrastructure.Security;

public sealed class EmailEncryptionOptions
{
    public const string SectionName = "EmailEncryption";

    public string KeyBase64 { get; set; } = string.Empty;
}
