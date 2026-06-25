using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Norgerman.Cryptography.Scrypt;
using TweetNaclSharp;

namespace RcloneEncrypt.Core;

public sealed class RcloneCipher : IDisposable
{
    private const string FileMagic = "RCLONE\0\0";
    private const int FileMagicSize = 8;
    private const int FileNonceSize = 24;
    private const int FileHeaderSize = FileMagicSize + FileNonceSize;
    private const int BlockDataSize = 64 * 1024;
    private static readonly int BlockHeaderSize = Nacl.SecretboxOverheadLength; // 16
    private static readonly int BlockSize = BlockHeaderSize + BlockDataSize;

    private static readonly ReadOnlyMemory<byte> DefaultSalt = new byte[]
    {
        0xA8, 0x0D, 0xF4, 0x3A, 0x8F, 0xBD, 0x03, 0x08,
        0xA7, 0xCA, 0xB8, 0x3E, 0x58, 0x1F, 0x86, 0xB1
    };

    private readonly byte[] _dataKey = new byte[Nacl.SecretboxKeyLength];
    private readonly byte[] _nameKey = new byte[32];
    private readonly byte[] _nameTweak = new byte[16];
    private readonly Aes _nameCipher;
    private readonly FilenameEncoding _filenameEncoding;
    private bool _disposed;

    public RcloneCipher(string password, string? salt = null, FilenameEncoding filenameEncoding = FilenameEncoding.Base32)
    {
        ArgumentNullException.ThrowIfNull(password);

        _filenameEncoding = filenameEncoding;

        byte[] saltBytes = string.IsNullOrEmpty(salt)
            ? DefaultSalt.ToArray()
            : Encoding.UTF8.GetBytes(salt);

        const int keySize = 32 + 32 + 16;
        byte[] key;

        try
        {
            if (password.Length == 0)
            {
                key = new byte[keySize];
            }
            else
            {
                key = ScryptUtil.Scrypt(password, saltBytes, 16384, 8, 1, keySize);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(saltBytes);
        }

        try
        {
            key.AsSpan(0, 32).CopyTo(_dataKey);
            key.AsSpan(32, 32).CopyTo(_nameKey);
            key.AsSpan(64, 16).CopyTo(_nameTweak);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        _nameCipher = Aes.Create();
        _nameCipher.Key = _nameKey;
        _nameCipher.Mode = CipherMode.ECB;
        _nameCipher.Padding = PaddingMode.None;
    }

    public byte[] EncryptData(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var inputStream = new MemoryStream(data.ToArray(), writable: false);
        using var outputStream = new MemoryStream();

        Span<byte> header = stackalloc byte[FileHeaderSize];
        Encoding.ASCII.GetBytes(FileMagic).CopyTo(header.Slice(0, FileMagicSize));

        byte[] nonce = Nacl.RandomBytes(FileNonceSize);
        nonce.CopyTo(header.Slice(FileMagicSize, FileNonceSize));

        outputStream.Write(header);

        byte[] readBuffer = ArrayPool<byte>.Shared.Rent(BlockDataSize);
        byte[] encryptBuffer = ArrayPool<byte>.Shared.Rent(BlockSize);

        try
        {
            int bytesRead;
            while ((bytesRead = inputStream.Read(readBuffer, 0, BlockDataSize)) > 0)
            {
                byte[] plaintext = readBuffer.AsSpan(0, bytesRead).ToArray();
                byte[] encrypted;
                try
                {
                    encrypted = Nacl.Secretbox(plaintext, nonce, _dataKey)
                        ?? throw new RcloneCryptException("Encryption failed.");
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                }

                try
                {
                    outputStream.Write(encrypted);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(encrypted);
                }

                IncrementNonce(nonce);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(readBuffer);
            CryptographicOperations.ZeroMemory(encryptBuffer);
            ArrayPool<byte>.Shared.Return(readBuffer);
            ArrayPool<byte>.Shared.Return(encryptBuffer);
            CryptographicOperations.ZeroMemory(nonce);
        }

        return outputStream.ToArray();
    }

    public byte[] DecryptData(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (data.Length < FileHeaderSize)
        {
            throw new RcloneCryptException("File is too short to be encrypted.");
        }

        ReadOnlySpan<byte> header = data.Slice(0, FileHeaderSize);
        ReadOnlySpan<byte> magicBytes = header.Slice(0, FileMagicSize);

        if (!magicBytes.SequenceEqual(Encoding.ASCII.GetBytes(FileMagic)))
        {
            throw new RcloneCryptException("Not an encrypted file - bad magic string.");
        }

        byte[] nonce = header.Slice(FileMagicSize, FileNonceSize).ToArray();

        try
        {
            using var outputStream = new MemoryStream();
            int offset = FileHeaderSize;

            while (offset < data.Length)
            {
                int blockLength = Math.Min(BlockSize, data.Length - offset);

                if (blockLength <= BlockHeaderSize)
                {
                    throw new RcloneCryptException("File has truncated block header.");
                }

                byte[] block = data.Slice(offset, blockLength).ToArray();

                byte[]? decrypted;
                try
                {
                    decrypted = Nacl.SecretboxOpen(block, nonce, _dataKey);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(block);
                }

                if (decrypted == null)
                {
                    throw new RcloneCryptException("Failed to authenticate decrypted block - bad password?");
                }

                try
                {
                    outputStream.Write(decrypted);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(decrypted);
                }
                IncrementNonce(nonce);
                offset += blockLength;
            }

            return outputStream.ToArray();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(nonce);
        }
    }

    public string EncryptFileName(string fileName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(fileName))
        {
            return fileName;
        }

        string[] segments = fileName.Split('/');
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = EncryptSegment(segments[i]);
        }

        return string.Join("/", segments);
    }

    public string DecryptFileName(string fileName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(fileName))
        {
            return fileName;
        }

        string[] segments = fileName.Split('/');
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = DecryptSegment(segments[i]);
        }

        return string.Join("/", segments);
    }

    private string EncryptSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
        {
            return segment;
        }

        byte[] padded = Pkcs7.Pad(16, Encoding.UTF8.GetBytes(segment));
        byte[] encrypted;

        try
        {
            encrypted = EmeTransform.Transform(_nameCipher, _nameTweak, padded, encrypt: true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(padded);
        }

        try
        {
            return _filenameEncoding switch
            {
                FilenameEncoding.Base32 => RcloneBase32Encoding.EncodeToString(encrypted),
                FilenameEncoding.Base64 => Convert.ToBase64String(encrypted).Replace("+", "-").Replace("/", "_").TrimEnd('='),
                FilenameEncoding.Base32768 => throw new NotSupportedException("Base32768 encoding is not supported."),
                _ => throw new NotSupportedException($"Encoding '{_filenameEncoding}' is not supported.")
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encrypted);
        }
    }

    private string DecryptSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
        {
            return segment;
        }

        byte[] encrypted = _filenameEncoding switch
        {
            FilenameEncoding.Base32 => RcloneBase32Encoding.DecodeFromString(segment),
            FilenameEncoding.Base64 => DecodeBase64Url(segment),
            FilenameEncoding.Base32768 => throw new NotSupportedException("Base32768 encoding is not supported."),
            _ => throw new NotSupportedException($"Encoding '{_filenameEncoding}' is not supported.")
        };

        if (encrypted.Length % 16 != 0)
        {
            throw new RcloneCryptException("Not a multiple of blocksize.");
        }

        byte[] decrypted;
        try
        {
            decrypted = EmeTransform.Transform(_nameCipher, _nameTweak, encrypted, encrypt: false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encrypted);
        }

        try
        {
            byte[] unpadded = Pkcs7.Unpad(16, decrypted);
            return Encoding.UTF8.GetString(unpadded);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decrypted);
        }
    }

    private static byte[] DecodeBase64Url(string segment)
    {
        string normalized = segment.Replace("-", "+").Replace("_", "/");
        int remainder = normalized.Length % 4;
        if (remainder > 0)
        {
            normalized += new string('=', 4 - remainder);
        }

        return Convert.FromBase64String(normalized);
    }

    private static void IncrementNonce(Span<byte> nonce)
    {
        for (int i = 0; i < nonce.Length; i++)
        {
            byte digit = nonce[i];
            byte newDigit = (byte)(digit + 1);
            nonce[i] = newDigit;
            if (newDigit >= digit)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_dataKey);
        CryptographicOperations.ZeroMemory(_nameKey);
        CryptographicOperations.ZeroMemory(_nameTweak);
        _nameCipher.Dispose();
        _disposed = true;
    }
}
