using VeilleBoisee.Application.Abstractions;

namespace VeilleBoisee.Application.Tests.Fakes;

internal sealed class FakeEmailEncryptionService : IEmailEncryptionService
{
    public string Encrypt(string plaintext) => $"encrypted:{plaintext}";
}
