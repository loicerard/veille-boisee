using System.Text.RegularExpressions;
using VeilleBoisee.Domain.Common;

namespace VeilleBoisee.Domain.ValueObjects;

public sealed partial record CodeInsee
{
    private const string InseePattern = @"^(\d{5}|2[AB]\d{3})$";

    [GeneratedRegex(InseePattern)]
    private static partial Regex InseeRegex();

    public string Value { get; }

    private CodeInsee(string value) => Value = value;

    public static Result<CodeInsee, string> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "CodeInsee cannot be empty.";
        }

        return InseeRegex().IsMatch(value)
            ? new CodeInsee(value)
            : "CodeInsee must be 5 digits or Corsican format (2A/2B + 3 digits).";
    }

    public override string ToString() => Value;
}
