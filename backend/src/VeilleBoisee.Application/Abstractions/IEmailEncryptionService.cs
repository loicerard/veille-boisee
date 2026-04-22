namespace VeilleBoisee.Application.Abstractions;

public interface IEmailEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
