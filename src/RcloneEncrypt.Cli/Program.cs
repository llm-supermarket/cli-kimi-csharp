using System.Security.Cryptography;
using System.Text;
using CommandLine;
using RcloneEncrypt.Core;

namespace RcloneEncrypt.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<EncryptOptions, DecryptOptions, EncryptNameOptions, DecryptNameOptions>(args)
            .MapResult(
                (EncryptOptions opts) => RunEncryptDecrypt(opts, encrypt: true),
                (DecryptOptions opts) => RunEncryptDecrypt(opts, encrypt: false),
                (EncryptNameOptions opts) => RunEncryptDecryptName(opts, encrypt: true),
                (DecryptNameOptions opts) => RunEncryptDecryptName(opts, encrypt: false),
                errors => 1);
    }

    private static int RunEncryptDecrypt(OptionsBase options, bool encrypt)
    {
        string inputFile = ((dynamic)options).InputFile;
        string? outputFile = ((dynamic)options).OutputFile;

        if (!File.Exists(inputFile))
        {
            Console.Error.WriteLine($"Error: input file not found: {inputFile}");
            return 1;
        }

        string? password = ResolvePassword(options.Password);
        if (password == null)
        {
            Console.Error.WriteLine("Error: password is required.");
            return 1;
        }

        if (!TryParseFilenameEncoding(options.FilenameEncoding, out FilenameEncoding encoding))
        {
            return 1;
        }

        byte[] inputBytes = File.ReadAllBytes(inputFile);
        byte[] outputBytes;

        try
        {
            using var cipher = new RcloneCipher(password, options.Salt, encoding);
            outputBytes = encrypt ? cipher.EncryptData(inputBytes) : cipher.DecryptData(inputBytes);
        }
        catch (RcloneCryptException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(inputBytes);
        }

        try
        {
            if (outputFile != null)
            {
                File.WriteAllBytes(outputFile, outputBytes);
            }
            else
            {
                using var stdout = Console.OpenStandardOutput();
                stdout.Write(outputBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(outputBytes);
        }

        return 0;
    }

    private static int RunEncryptDecryptName(OptionsBase options, bool encrypt)
    {
        string fileName = ((dynamic)options).FileName;

        string? password = ResolvePassword(options.Password);
        if (password == null)
        {
            Console.Error.WriteLine("Error: password is required.");
            return 1;
        }

        if (!TryParseFilenameEncoding(options.FilenameEncoding, out FilenameEncoding encoding))
        {
            return 1;
        }

        using var cipher = new RcloneCipher(password, options.Salt, encoding);

        string result = encrypt ? cipher.EncryptFileName(fileName) : cipher.DecryptFileName(fileName);
        Console.WriteLine(result);

        return 0;
    }

    private static bool TryParseFilenameEncoding(string value, out FilenameEncoding encoding)
    {
        if (Enum.TryParse(value, ignoreCase: true, out encoding))
        {
            return true;
        }

        Console.Error.WriteLine($"Error: invalid filename encoding '{value}'. Supported values: base32, base64.");
        encoding = FilenameEncoding.Base32;
        return false;
    }

    private static string? ResolvePassword(string? cliPassword)
    {
        if (!string.IsNullOrEmpty(cliPassword))
        {
            Console.Error.WriteLine("Warning: passing --password on the command line exposes the password in your shell history.");
            Console.Error.WriteLine("         Consider using the RCLONE_ENCRYPT_PASSWORD environment variable instead,");
            Console.Error.WriteLine("         and clear your terminal history if you must use this flag.");
            return cliPassword;
        }

        string? envPassword = Environment.GetEnvironmentVariable("RCLONE_ENCRYPT_PASSWORD");
        if (!string.IsNullOrEmpty(envPassword))
        {
            return envPassword;
        }

        Console.Error.Write("Password: ");
        return ReadPassword();
    }

    private static string? ReadPassword()
    {
        if (Console.IsInputRedirected)
        {
            string? line = Console.ReadLine();
            return string.IsNullOrWhiteSpace(line) ? null : line.TrimEnd();
        }

        var builder = new StringBuilder();
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.Error.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace && builder.Length > 0)
            {
                builder.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                builder.Append(key.KeyChar);
            }
        }
        while (key.Key != ConsoleKey.Enter);

        return builder.Length > 0 ? builder.ToString() : null;
    }
}
