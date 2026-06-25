using CommandLine;
using RcloneEncrypt.Core;

namespace RcloneEncrypt.Cli;

internal abstract class OptionsBase
{
    [Option('p', "password", Required = false, HelpText = "Password for encryption/decryption. WARNING: passing passwords on the command line exposes them in shell history. Consider using the RCLONE_ENCRYPT_PASSWORD environment variable instead, and clear your terminal history if you must use this flag.")]
    public string? Password { get; set; }

    [Option('s', "salt", Required = false, HelpText = "Optional salt. If omitted, the rclone default salt is used.")]
    public string? Salt { get; set; }

    [Option('e', "filename-encoding", Required = false, Default = "base32", HelpText = "Filename encoding: base32 or base64.")]
    public string FilenameEncoding { get; set; } = "base32";
}

[Verb("encrypt", HelpText = "Encrypt a file using rclone crypt defaults.")]
internal class EncryptOptions : OptionsBase
{
    [Option('i', "input-file", Required = true, HelpText = "Input file path.")]
    public string InputFile { get; set; } = string.Empty;

    [Option('o', "output-file", Required = false, HelpText = "Output file path. If omitted, output is written to stdout.")]
    public string? OutputFile { get; set; }
}

[Verb("decrypt", HelpText = "Decrypt a file using rclone crypt defaults.")]
internal class DecryptOptions : OptionsBase
{
    [Option('i', "input-file", Required = true, HelpText = "Input file path.")]
    public string InputFile { get; set; } = string.Empty;

    [Option('o', "output-file", Required = false, HelpText = "Output file path. If omitted, output is written to stdout.")]
    public string? OutputFile { get; set; }
}

[Verb("encrypt-name", HelpText = "Encrypt a filename using rclone crypt defaults.")]
internal class EncryptNameOptions : OptionsBase
{
    [Value(0, Required = true, HelpText = "Filename to encrypt.")]
    public string FileName { get; set; } = string.Empty;
}

[Verb("decrypt-name", HelpText = "Decrypt a filename using rclone crypt defaults.")]
internal class DecryptNameOptions : OptionsBase
{
    [Value(0, Required = true, HelpText = "Filename to decrypt.")]
    public string FileName { get; set; } = string.Empty;
}
