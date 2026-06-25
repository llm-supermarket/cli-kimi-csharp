using System.Security.Cryptography;

namespace RcloneEncrypt.Core;

internal static class EmeTransform
{
    public static byte[] Transform(Aes blockCipher, ReadOnlySpan<byte> tweak, ReadOnlySpan<byte> inputData, bool encrypt)
    {
        if (blockCipher.BlockSize != 128)
        {
            throw new ArgumentException("Using a block size other than 128 is not implemented.", nameof(blockCipher));
        }

        if (tweak.Length != 16)
        {
            throw new ArgumentException("Tweak must be 16 bytes long.", nameof(tweak));
        }

        if (inputData.Length % 16 != 0)
        {
            throw new ArgumentException("Data must be a multiple of 16 bytes long.", nameof(inputData));
        }

        int m = inputData.Length / 16;
        if (m == 0 || m > 16 * 8)
        {
            throw new ArgumentException($"EME operates on 1 to {16 * 8} block-cipher blocks.");
        }

        byte[] lTable = TabulateL(blockCipher, m);
        byte[] c = new byte[inputData.Length];

        // PPj = 2**(j-1)*L xor Pj
        // PPPj = AESenc(K; PPj)
        byte[] ppj = new byte[16];
        for (int j = 0; j < m; j++)
        {
            ReadOnlySpan<byte> pj = inputData.Slice(j * 16, 16);
            XorBlocks(ppj, pj, lTable.AsSpan(j * 16, 16));
            AesTransform(c.AsSpan(j * 16, 16), ppj, encrypt, blockCipher);
        }

        // MP = (xorSum PPPj) xor T
        byte[] mp = new byte[16];
        XorBlocks(mp, c.AsSpan(0, 16), tweak);
        for (int j = 1; j < m; j++)
        {
            XorBlocks(mp, mp, c.AsSpan(j * 16, 16));
        }

        // MC = AESenc(K; MP)
        byte[] mc = new byte[16];
        AesTransform(mc, mp, encrypt, blockCipher);

        // M = MP xor MC
        byte[] mBuffer = new byte[16];
        XorBlocks(mBuffer, mp, mc);

        // CCCj = 2**(j-1)*M xor PPPj
        byte[] cccj = new byte[16];
        for (int j = 1; j < m; j++)
        {
            MultByTwo(mBuffer, mBuffer);
            XorBlocks(cccj, c.AsSpan(j * 16, 16), mBuffer);
            cccj.CopyTo(c.AsSpan(j * 16, 16));
        }

        // CCC1 = (xorSum CCCj) xor T xor MC
        byte[] ccc1 = new byte[16];
        XorBlocks(ccc1, mc, tweak);
        for (int j = 1; j < m; j++)
        {
            XorBlocks(ccc1, ccc1, c.AsSpan(j * 16, 16));
        }
        ccc1.CopyTo(c.AsSpan(0, 16));

        // CCj = AES-enc(K; CCCj)
        // Cj = 2**(j-1)*L xor CCj
        for (int j = 0; j < m; j++)
        {
            AesTransform(c.AsSpan(j * 16, 16), c.AsSpan(j * 16, 16), encrypt, blockCipher);
            XorBlocks(c.AsSpan(j * 16, 16), c.AsSpan(j * 16, 16), lTable.AsSpan(j * 16, 16));
        }

        CryptographicOperations.ZeroMemory(lTable);
        CryptographicOperations.ZeroMemory(ppj);
        CryptographicOperations.ZeroMemory(mp);
        CryptographicOperations.ZeroMemory(mc);
        CryptographicOperations.ZeroMemory(mBuffer);
        CryptographicOperations.ZeroMemory(cccj);
        CryptographicOperations.ZeroMemory(ccc1);

        return c;
    }

    private static byte[] TabulateL(Aes blockCipher, int m)
    {
        byte[] eZero = new byte[16];
        byte[] li = new byte[16];
        blockCipher.EncryptEcb(eZero, li, PaddingMode.None);

        byte[] lTable = new byte[m * 16];
        for (int i = 0; i < m; i++)
        {
            MultByTwo(li, li);
            li.CopyTo(lTable.AsSpan(i * 16, 16));
        }

        CryptographicOperations.ZeroMemory(eZero);
        CryptographicOperations.ZeroMemory(li);

        return lTable;
    }

    private static void MultByTwo(Span<byte> output, ReadOnlySpan<byte> input)
    {
        if (input.Length != 16 || output.Length != 16)
        {
            throw new ArgumentException("Length must be 16.");
        }

        byte[] tmp = new byte[16];
        tmp[0] = (byte)(2 * input[0]);
        tmp[0] ^= (byte)(135 & -(input[15] >> 7));

        for (int j = 1; j < 16; j++)
        {
            tmp[j] = (byte)(2 * input[j]);
            tmp[j] += (byte)(input[j - 1] >> 7);
        }

        tmp.CopyTo(output);
    }

    private static void XorBlocks(Span<byte> output, ReadOnlySpan<byte> input1, ReadOnlySpan<byte> input2)
    {
        if (input1.Length != input2.Length || output.Length != input1.Length)
        {
            throw new ArgumentException("Block lengths must match.");
        }

        for (int i = 0; i < input1.Length; i++)
        {
            output[i] = (byte)(input1[i] ^ input2[i]);
        }
    }

    private static void XorBlocks(Span<byte> output, ReadOnlySpan<byte> input)
    {
        if (output.Length != input.Length)
        {
            throw new ArgumentException("Block lengths must match.");
        }

        for (int i = 0; i < input.Length; i++)
        {
            output[i] ^= input[i];
        }
    }

    private static void AesTransform(Span<byte> destination, ReadOnlySpan<byte> source, bool encrypt, Aes blockCipher)
    {
        if (encrypt)
        {
            blockCipher.EncryptEcb(source, destination, PaddingMode.None);
        }
        else
        {
            blockCipher.DecryptEcb(source, destination, PaddingMode.None);
        }
    }
}
