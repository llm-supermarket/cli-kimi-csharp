using System.Diagnostics;
using System.Text;
using FluentAssertions;

namespace RcloneEncrypt.Tests;

public class CliTests : IDisposable
{
    private readonly string _cliPath;
    private readonly string _projectPath;
    private readonly string _repoRoot;
    private readonly List<string> _tempFiles = [];

    public CliTests()
    {
        _repoRoot = GetRepoRoot();
        _projectPath = Path.Combine(_repoRoot, "src", "RcloneEncrypt.Cli", "RcloneEncrypt.Cli.csproj");

        string configuration = GetBuildConfiguration();
        _cliPath = Path.Combine(_repoRoot, "src", "RcloneEncrypt.Cli", "bin", configuration, "net10.0", "rclone-encrypt-test-kimi-csharp.dll");
    }

    public void Dispose()
    {
        foreach (string file in _tempFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors.
            }
        }
    }

    [Fact]
    public void DecryptBase32File_WithPassword_ProducesExpectedContent()
    {
        string inputFile = Path.Combine(_repoRoot, "kr9tu4e1da4u3nifdd99g9tf5o");
        string outputFile = GetTempFilePath();

        int exitCode = RunCli("decrypt", $"--input-file \"{inputFile}\" --output-file \"{outputFile}\" --password Testpassword1");
        exitCode.Should().Be(0);

        string content = File.ReadAllText(outputFile);
        content.Should().Contain("umbrella top kit charge");
    }

    [Fact]
    public void DecryptBase64File_WithPasswordAndBase64Encoding_ProducesExpectedContent()
    {
        string inputFile = Path.Combine(_repoRoot, "Iyxcijgc9bp3o5Y0npW6xqUvwWNcc3MA4SadB0sR6cY");
        string outputFile = GetTempFilePath();

        int exitCode = RunCli("decrypt", $"--input-file \"{inputFile}\" --output-file \"{outputFile}\" --password Testpassword1 --filename-encoding base64");
        exitCode.Should().Be(0);

        string content = File.ReadAllText(outputFile);
        content.Should().Contain("umbrella top kit charge");
    }

    [Fact]
    public void DecryptBase32File_WithPasswordPrompt_ProducesExpectedContent()
    {
        string inputFile = Path.Combine(_repoRoot, "kr9tu4e1da4u3nifdd99g9tf5o");
        string outputFile = GetTempFilePath();

        int exitCode = RunCliWithInput("Testpassword1\n", "decrypt", $"--input-file \"{inputFile}\" --output-file \"{outputFile}\"");
        exitCode.Should().Be(0);

        string content = File.ReadAllText(outputFile);
        content.Should().Contain("umbrella top kit charge");
    }

    [Fact]
    public void DecryptBase32Name_WithPassword_ProducesTestFile()
    {
        int exitCode = RunCli("decrypt-name", "kr9tu4e1da4u3nifdd99g9tf5o --password Testpassword1");
        exitCode.Should().Be(0);
    }

    [Fact]
    public void DecryptBase64Name_WithPasswordAndBase64Encoding_ProducesExpectedName()
    {
        var (exitCode, output) = RunCliWithOutput("decrypt-name", "Iyxcijgc9bp3o5Y0npW6xqUvwWNcc3MA4SadB0sR6cY --password Testpassword1 --filename-encoding base64");
        exitCode.Should().Be(0);
        output.Trim().Should().Be("TEST_FILE BASE64.txt");
    }

    [Fact]
    public void EncryptDecryptRoundTrip_WithoutSalt_ProducesOriginalContent()
    {
        string originalContent = "Hello, rclone crypt world!";
        string inputFile = GetTempFilePath();
        string encryptedFile = GetTempFilePath();
        string decryptedFile = GetTempFilePath();
        File.WriteAllText(inputFile, originalContent);

        int encryptExit = RunCli("encrypt", $"--input-file \"{inputFile}\" --output-file \"{encryptedFile}\" --password Testpassword1");
        encryptExit.Should().Be(0);

        int decryptExit = RunCli("decrypt", $"--input-file \"{encryptedFile}\" --output-file \"{decryptedFile}\" --password Testpassword1");
        decryptExit.Should().Be(0);

        File.ReadAllText(decryptedFile).Should().Be(originalContent);
    }

    [Fact]
    public void EncryptDecryptRoundTrip_WithSalt_ProducesOriginalContent()
    {
        string originalContent = "Salted rclone crypt content.";
        string inputFile = GetTempFilePath();
        string encryptedFile = GetTempFilePath();
        string decryptedFile = GetTempFilePath();
        File.WriteAllText(inputFile, originalContent);

        int encryptExit = RunCli("encrypt", $"--input-file \"{inputFile}\" --output-file \"{encryptedFile}\" --password Testpassword1 --salt MySalt");
        encryptExit.Should().Be(0);

        int decryptExit = RunCli("decrypt", $"--input-file \"{encryptedFile}\" --output-file \"{decryptedFile}\" --password Testpassword1 --salt MySalt");
        decryptExit.Should().Be(0);

        File.ReadAllText(decryptedFile).Should().Be(originalContent);
    }

    [Fact]
    public void EncryptDecryptNameRoundTrip_WithBase64Encoding_ProducesOriginalName()
    {
        string originalName = "important-document.pdf";

        var (encryptExit, encryptedName) = RunCliWithOutput("encrypt-name", $"\"{originalName}\" --password Testpassword1 --filename-encoding base64");
        encryptExit.Should().Be(0);
        encryptedName.Trim().Should().NotBe(originalName);

        var (decryptExit, decryptedName) = RunCliWithOutput("decrypt-name", $"\"{encryptedName.Trim()}\" --password Testpassword1 --filename-encoding base64");
        decryptExit.Should().Be(0);
        decryptedName.Trim().Should().Be(originalName);
    }

    [Fact]
    public void PasswordFlag_WritesSecurityWarning()
    {
        string inputFile = Path.Combine(_repoRoot, "kr9tu4e1da4u3nifdd99g9tf5o");
        string outputFile = GetTempFilePath();

        var (exitCode, _, error) = RunCliWithOutputAndError("decrypt", $"--input-file \"{inputFile}\" --output-file \"{outputFile}\" --password Testpassword1");
        exitCode.Should().Be(0);
        error.Should().Contain("Warning: passing --password on the command line");
    }

    private int RunCli(string command, string args)
    {
        return RunCliWithInput(null, command, args);
    }

    private int RunCliWithInput(string? input, string command, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{_cliPath}\" {command} {args}",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = input != null,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;

        if (input != null)
        {
            process.StandardInput.Write(input);
            process.StandardInput.Close();
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    private (int ExitCode, string Output) RunCliWithOutput(string command, string args)
    {
        var result = RunCliWithOutputAndError(command, args);
        return (result.ExitCode, result.Output);
    }

    private (int ExitCode, string Output, string Error) RunCliWithOutputAndError(string command, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{_cliPath}\" {command} {args}",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output, error);
    }

    private string GetTempFilePath()
    {
        string path = Path.Combine(Path.GetTempPath(), $"rclone-encrypt-test-{Guid.NewGuid()}.tmp");
        _tempFiles.Add(path);
        return path;
    }

    private static string GetRepoRoot()
    {
        string? directory = new DirectoryInfo(Directory.GetCurrentDirectory()).FullName;
        while (directory != null && !File.Exists(Path.Combine(directory, "README.md")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        return directory ?? throw new InvalidOperationException("Could not find repository root.");
    }

    private static string GetBuildConfiguration()
    {
        string assemblyLocation = AppContext.BaseDirectory;
        if (assemblyLocation.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return "Release";
        }

        return "Debug";
    }
}
