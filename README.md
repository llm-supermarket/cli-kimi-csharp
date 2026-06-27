# cli-kimi-csharp
A small CLI tool that encrypts and decrypts using the rclone encryption defaults.

Rclone uses a custom salt if no salt is provided, which this tool will use by default. A few similar tools:

- https://github.com/rclone/rclone
- https://github.com/mcolatosti/rclonedecrypt
- https://github.com/br0kenpixel/rclone-rcc
- @fyears/rclone-crypt

Rclone encryption uses: 
- NaCl SecretBox (XSalsa20 + Poly1305) for the file contents.
- AES256 for the filenames.
- scrypt for keymaterial.

## Installation

**Homebrew (macOS/Linux)**
```bash
brew tap llm-supermarket/cli-kimi-csharp https://github.com/llm-supermarket/cli-kimi-csharp
brew install cli-kimi-csharp
```

**Scoop (Windows)**
```powershell
scoop bucket add cli-kimi-csharp https://github.com/llm-supermarket/cli-kimi-csharp
scoop install cli-kimi-csharp
```

## Usage

The tool uses subcommands to encrypt/decrypt file contents and filenames. If `--password` is omitted, you will be prompted securely.

> **Security note:** passing `--password` on the command line exposes the password in your shell history. Consider using the `RCLONE_ENCRYPT_PASSWORD` environment variable instead, and clear your terminal history if you must use the flag.

### Encrypt a file

```bash
cli-kimi-csharp encrypt --input-file secret.txt --output-file secret.txt.bin
# or with a custom salt
cli-kimi-csharp encrypt --input-file secret.txt --output-file secret.txt.bin --salt my-salt
```

### Decrypt a file

```bash
cli-kimi-csharp decrypt --input-file secret.txt.bin --output-file secret.txt
```

### Encrypt/decrypt filenames

Filenames are encoded with **base32** by default. Use `--filename-encoding base64` for shorter names.

```bash
cli-kimi-csharp encrypt-name "important-document.pdf"
# base64 example
cli-kimi-csharp encrypt-name "important-document.pdf" --filename-encoding base64

cli-kimi-csharp decrypt-name "kr9tu4e1da4u3nifdd99g9tf5o"
cli-kimi-csharp decrypt-name "Iyxcijgc9bp3o5Y0npW6xqUvwWNcc3MA4SadB0sR6cY" --filename-encoding base64
```

### Using environment variables

```bash
export RCLONE_ENCRYPT_PASSWORD="Testpassword1"
cli-kimi-csharp decrypt --input-file kr9tu4e1da4u3nifdd99g9tf5o --output-file TEST_FILE.txt
```

## Commands

| Command | Description |
|---------|-------------|
| `encrypt` | Encrypt a file's contents. |
| `decrypt` | Decrypt a file's contents. |
| `encrypt-name` | Encrypt a filename. |
| `decrypt-name` | Decrypt a filename. |

## Flags

| Flag | Default | Description |
|------|---------|-------------|
| `-i`, `--input-file` | *(required for encrypt/decrypt)* | Path to the input file. |
| `-o`, `--output-file` | *(stdout)* | Path to the output file. If omitted, output is written to stdout. |
| `-p`, `--password` | *(prompt)* | Password. Passing this on the command line is insecure. |
| `-s`, `--salt` | *(rclone default)* | Optional salt. If omitted, the rclone default salt is used. |
| `-e`, `--filename-encoding` | `base32` | Filename encoding: `base32` or `base64`. |
| `--help` | | Show help for a command. |

## Building from Source

Requires .NET 10 SDK.

```bash
git clone https://github.com/llm-supermarket/cli-kimi-csharp
cd cli-kimi-csharp
dotnet build
dotnet test
```

## Releases

Pushing a `vX.Y.Z` tag triggers the [Build and Release workflow](.github/workflows/build-release.yml), which cross-compiles binaries for Linux and macOS (amd64/arm64) and Windows (amd64), publishes a GitHub Release, and updates the Scoop manifest (`cli-kimi-csharp.json`) and Homebrew formula (`Formula/cli-kimi-csharp.rb`) in this repo.
