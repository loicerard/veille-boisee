using VeilleBoisee.Application.Abstractions;

namespace VeilleBoisee.Application.Tests.Fakes;

internal sealed class FakeEmailEncryptionService : IEmailEncryptionService
{
    public string Encrypt(string plaintext) => $"encrypted:{plaintext}";
    public string Decrypt(string ciphertext) => ciphertext.StartsWith("encrypted:", StringComparison.Ordinal) ? ciphertext["encrypted:".Length..] : ciphertext;
}
