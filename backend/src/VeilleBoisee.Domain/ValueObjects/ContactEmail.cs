using System.Text.RegularExpressions;
using VeilleBoisee.Domain.Common;

namespace VeilleBoisee.Domain.ValueObjects;

public sealed partial record ContactEmail
{
    public const int MaxLength = 320;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public string Value { get; }

    private ContactEmail(string value) => Value = value;

    public static Result<ContactEmail, string> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "L'email ne peut pas être vide.";

        if (value.Length > MaxLength)
            return $"L'email ne peut pas dépasser {MaxLength} caractères.";

        return EmailRegex().IsMatch(value)
            ? new ContactEmail(value)
            : "Le format de l'email est invalide.";
    }

    public override string ToString() => Value;
}
