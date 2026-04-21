using FluentAssertions;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Domain.Tests.ValueObjects;

public class CodeInseeTests
{
    [Theory]
    [InlineData("75056")]   // Paris
    [InlineData("13055")]   // Marseille
    [InlineData("2A004")]   // Ajaccio (Corse du Sud)
    [InlineData("2B033")]   // Bastia (Haute-Corse)
    [InlineData("97101")]   // Guadeloupe
    public void Create_accepts_valid_insee_codes(string value)
    {
        var result = CodeInsee.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("1234")]        // Too short
    [InlineData("123456")]      // Too long
    [InlineData("ABCDE")]       // Letters outside Corsican prefix
    [InlineData("2C123")]       // Invalid Corsican prefix
    [InlineData("75O56")]       // Letter O instead of zero
    public void Create_rejects_invalid_insee_codes(string value)
    {
        var result = CodeInsee.Create(value);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_null()
    {
        var result = CodeInsee.Create(null!);

        result.IsFailure.Should().BeTrue();
    }
}
