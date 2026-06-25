namespace RcloneEncrypt.Core;

internal static class Pkcs7
{
    public static byte[] Pad(int blockSize, ReadOnlySpan<byte> data)
    {
        int padding = blockSize - (data.Length % blockSize);
        byte[] padded = new byte[data.Length + padding];
        data.CopyTo(padded);
        padded.AsSpan(data.Length).Fill((byte)padding);
        return padded;
    }

    public static byte[] Unpad(int blockSize, ReadOnlySpan<byte> data)
    {
        if (data.Length == 0 || data.Length % blockSize != 0)
        {
            throw new RcloneCryptException("Invalid PKCS7 padding.");
        }

        int padding = data[^1];
        if (padding == 0 || padding > blockSize)
        {
            throw new RcloneCryptException("Invalid PKCS7 padding.");
        }

        for (int i = 0; i < padding; i++)
        {
            if (data[data.Length - 1 - i] != padding)
            {
                throw new RcloneCryptException("Invalid PKCS7 padding.");
            }
        }

        return data.Slice(0, data.Length - padding).ToArray();
    }
}
