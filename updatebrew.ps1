param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$repo = "llm-supermarket/rclone-encrypt-test-kimi-csharp"
$platforms = @("darwin-amd64", "darwin-arm64", "linux-amd64", "linux-arm64")
$formulaPath = "$PSScriptRoot/Formula/rclone-encrypt-test-kimi-csharp.rb"
$base = "https://github.com/$repo/releases/download/v$Version"

$hash = @{}
foreach ($platform in $platforms) {
    $url = "$base/rclone-encrypt-test-kimi-csharp-$platform.tar.gz"
    $tempFile = Join-Path ([System.IO.Path]::GetTempPath()) "rclone-encrypt-test-kimi-csharp-$platform.tar.gz"

    Write-Host "Downloading $url ..."
    Invoke-WebRequest -Uri $url -OutFile $tempFile

    $hash[$platform] = (Get-FileHash -Path $tempFile -Algorithm SHA256).Hash.ToLower()
    Write-Host "SHA256 for ${platform}: $($hash[$platform])"

    Remove-Item $tempFile
}

$formula = @"
class RcloneEncryptTestKimiCsharp < Formula
  desc "A small CLI tool that encrypts and decrypts using the rclone encryption defaults"
  homepage "https://github.com/$repo"
  version "$Version"

  on_macos do
    if Hardware::CPU.arm?
      url "$base/rclone-encrypt-test-kimi-csharp-darwin-arm64.tar.gz"
      sha256 "$($hash['darwin-arm64'])"
    else
      url "$base/rclone-encrypt-test-kimi-csharp-darwin-amd64.tar.gz"
      sha256 "$($hash['darwin-amd64'])"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "$base/rclone-encrypt-test-kimi-csharp-linux-arm64.tar.gz"
      sha256 "$($hash['linux-arm64'])"
    else
      url "$base/rclone-encrypt-test-kimi-csharp-linux-amd64.tar.gz"
      sha256 "$($hash['linux-amd64'])"
    end
  end

  def install
    bin.install "rclone-encrypt-test-kimi-csharp-darwin-arm64" => "rclone-encrypt-test-kimi-csharp" if OS.mac? && Hardware::CPU.arm?
    bin.install "rclone-encrypt-test-kimi-csharp-darwin-amd64" => "rclone-encrypt-test-kimi-csharp" if OS.mac? && !Hardware::CPU.arm?
    bin.install "rclone-encrypt-test-kimi-csharp-linux-arm64" => "rclone-encrypt-test-kimi-csharp" if OS.linux? && Hardware::CPU.arm?
    bin.install "rclone-encrypt-test-kimi-csharp-linux-amd64" => "rclone-encrypt-test-kimi-csharp" if OS.linux? && !Hardware::CPU.arm?
  end

  test do
    assert_match "rclone-encrypt-test-kimi-csharp #{version}", shell_output("#{bin}/rclone-encrypt-test-kimi-csharp --version")
  end
end
"@

Set-Content -Path $formulaPath -Value $formula -NoNewline
Write-Host "Wrote $formulaPath for version $Version"
