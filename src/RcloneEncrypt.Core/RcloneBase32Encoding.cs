namespace RcloneEncrypt.Core;

internal static class RcloneBase32Encoding
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    public static string EncodeToString(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
        {
            return string.Empty;
        }

        int outputLength = (source.Length * 8 + 4) / 5;
        char[] output = new char[outputLength];

        int bits = 0;
        int value = 0;
        int index = 0;

        foreach (byte b in source)
        {
            value = (value << 8) | b;
            bits += 8;

            while (bits >= 5)
            {
                output[index++] = Alphabet[(value >> (bits - 5)) & 0x1F];
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            output[index++] = Alphabet[(value << (5 - bits)) & 0x1F];
        }

        return new string(output);
    }

    public static byte[] DecodeFromString(ReadOnlySpan<char> source)
    {
        if (source.IsEmpty)
        {
            return [];
        }

        int outputLength = source.Length * 5 / 8;
        byte[] output = new byte[outputLength];

        int bits = 0;
        int value = 0;
        int index = 0;

        foreach (char c in source)
        {
            int decoded = DecodeChar(c);
            if (decoded < 0)
            {
                throw new RcloneCryptException("Bad base32 filename encoding.");
            }

            value = (value << 5) | decoded;
            bits += 5;

            if (bits >= 8)
            {
                output[index++] = (byte)((value >> (bits - 8)) & 0xFF);
                bits -= 8;
            }
        }

        return output;
    }

    private static int DecodeChar(char c)
    {
        if (c >= '0' && c <= '9')
        {
            return c - '0';
        }

        if (c >= 'a' && c <= 'z')
        {
            return c - 'a' + 10;
        }

        if (c >= 'A' && c <= 'Z')
        {
            return c - 'A' + 10;
        }

        return -1;
    }
}
