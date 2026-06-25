class RcloneEncryptTestKimiCsharp < Formula
  desc "A small CLI tool that encrypts and decrypts using the rclone encryption defaults"
  homepage "https://github.com/llm-supermarket/rclone-encrypt-test-kimi-csharp"
  version "1.0.0"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/rclone-encrypt-test-kimi-csharp/releases/download/v1.0.0/rclone-encrypt-test-kimi-csharp-darwin-arm64.tar.gz"
      sha256 "1230e759f25db5fecfdb11d00a164b300aa0682d2f9657330b2420eb8a91d90c"
    else
      url "https://github.com/llm-supermarket/rclone-encrypt-test-kimi-csharp/releases/download/v1.0.0/rclone-encrypt-test-kimi-csharp-darwin-amd64.tar.gz"
      sha256 "855095a5584c2c2cdbd7cbc82d600aea196a6346019f702a8c4f2a9e44221831"
    end
  end

  on_linux do
    if Hardware::CPU.arm?
      url "https://github.com/llm-supermarket/rclone-encrypt-test-kimi-csharp/releases/download/v1.0.0/rclone-encrypt-test-kimi-csharp-linux-arm64.tar.gz"
      sha256 "cd1820f5b1af09c401d06d400936e7026602d3709e7b9168fadf87007b862868"
    else
      url "https://github.com/llm-supermarket/rclone-encrypt-test-kimi-csharp/releases/download/v1.0.0/rclone-encrypt-test-kimi-csharp-linux-amd64.tar.gz"
      sha256 "c063a87e2c68d314122fc4e58f4195b4458711e36d1c28a26925cac90ffed683"
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
