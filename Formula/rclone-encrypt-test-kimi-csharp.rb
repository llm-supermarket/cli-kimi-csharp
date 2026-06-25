class RcloneEncryptTestKimiCsharp < Formula
  desc "A small CLI tool that encrypts and decrypts using the rclone encryption defaults"
  homepage "https://github.com/llm-supermarket/rclone-encrypt-test-kimi-csharp"
  version "1.0.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/rclone-encrypt-test-kimi-csharp/releases/download/v1.0.0/rclone-encrypt-test-kimi-csharp-darwin-arm64.tar.gz"
      sha256 "PLACEHOLDER"
    else
      url "https://github.com/llm-supermarket/rclone-encrypt-test-kimi-csharp/releases/download/v1.0.0/rclone-encrypt-test-kimi-csharp-darwin-amd64.tar.gz"
      sha256 "PLACEHOLDER"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/rclone-encrypt-test-kimi-csharp/releases/download/v1.0.0/rclone-encrypt-test-kimi-csharp-linux-arm64.tar.gz"
      sha256 "PLACEHOLDER"
    else
      url "https://github.com/llm-supermarket/rclone-encrypt-test-kimi-csharp/releases/download/v1.0.0/rclone-encrypt-test-kimi-csharp-linux-amd64.tar.gz"
      sha256 "PLACEHOLDER"
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
