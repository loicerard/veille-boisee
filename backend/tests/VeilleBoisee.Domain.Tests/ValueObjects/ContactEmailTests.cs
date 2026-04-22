using FluentAssertions;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Domain.Tests.ValueObjects;

public class ContactEmailTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("contact@mairie.fr")]
    [InlineData("signalement+foret@association.org")]
    [InlineData("a@b.io")]
    public void Create_accepts_valid_email_addresses(string value)
    {
        var result = ContactEmail.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("pasunemail")]
    [InlineData("@domaine.fr")]
    [InlineData("user@")]
    [InlineData("user @exemple.fr")]
    public void Create_rejects_invalid_email_addresses(string value)
    {
        var result = ContactEmail.Create(value);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_null()
    {
        var result = ContactEmail.Create(null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_email_exceeding_max_length()
    {
        var tooLong = new string('a', ContactEmail.MaxLength) + "@b.fr";

        var result = ContactEmail.Create(tooLong);

        result.IsFailure.Should().BeTrue();
    }
}
