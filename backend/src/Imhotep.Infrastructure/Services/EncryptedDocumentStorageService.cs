using Imhotep.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Imhotep.Infrastructure.Services;

public class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    public string RootPath { get; set; } = "storage/documents";
    /// <summary>Base64-encoded 256-bit AES key. In production, source from a KMS/secret store.</summary>
    public string EncryptionKeyBase64 { get; set; } = string.Empty;
}

/// <summary>
/// Stores documents encrypted at rest with AES-256-GCM (authenticated encryption).
/// File layout: [12-byte nonce][16-byte tag][ciphertext]. File names are random
/// GUIDs — never derived from user input, which rules out path traversal.
/// </summary>
public class EncryptedDocumentStorageService : IDocumentStorageService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly string _rootPath;
    private readonly byte[] _key;

    public EncryptedDocumentStorageService(IOptions<DocumentStorageOptions> options)
    {
        _rootPath = options.Value.RootPath;
        _key = Convert.FromBase64String(options.Value.EncryptionKeyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("DocumentStorage:EncryptionKeyBase64 must be a base64-encoded 256-bit key.");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var plaintext = buffer.ToArray();

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }
        CryptographicOperations.ZeroMemory(plaintext);

        var fileName = $"{Guid.NewGuid():N}.bin";
        var fullPath = Path.Combine(_rootPath, fileName);
        await using (var fs = File.Create(fullPath))
        {
            await fs.WriteAsync(nonce, ct);
            await fs.WriteAsync(tag, ct);
            await fs.WriteAsync(ciphertext, ct);
        }
        return fileName;
    }

    public async Task<byte[]> ReadAsync(string storagePath, CancellationToken ct)
    {
        // Defence in depth: a storage path is always a flat generated name.
        var fileName = Path.GetFileName(storagePath);
        var fullPath = Path.Combine(_rootPath, fileName);

        var raw = await File.ReadAllBytesAsync(fullPath, ct);
        var nonce = raw.AsSpan(0, NonceSize).ToArray();
        var tag = raw.AsSpan(NonceSize, TagSize).ToArray();
        var ciphertext = raw.AsSpan(NonceSize + TagSize).ToArray();

        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
