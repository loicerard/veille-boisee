using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using VeilleBoisee.Application.Abstractions;

namespace VeilleBoisee.Infrastructure.Security;

internal sealed class AesEmailEncryptionService : IEmailEncryptionService
{
    private readonly byte[] _key;

    public AesEmailEncryptionService(IOptions<EmailEncryptionOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.KeyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException(
                "EmailEncryption:KeyBase64 must be a 256-bit key (32 bytes, base64-encoded). " +
                "Generate one with: openssl rand -base64 32");
    }

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // Layout: [16-byte IV][ciphertext], base64-encoded
        var result = new byte[aes.IV.Length + ciphertextBytes.Length];
        aes.IV.CopyTo(result, 0);
        ciphertextBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }
}
