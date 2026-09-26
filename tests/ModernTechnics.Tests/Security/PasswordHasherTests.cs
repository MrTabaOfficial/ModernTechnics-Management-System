using ModernTechnics.Infrastructure.Security;

namespace ModernTechnics.Tests.Security;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 1_000);

    [Fact]
    public void Hash_does_not_contain_the_password()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.DoesNotContain("Secret123", hash);
        Assert.StartsWith("pbkdf2-sha256$1000$", hash);
    }

    [Fact]
    public void Same_password_hashes_differently_each_time()
    {
        Assert.NotEqual(_hasher.Hash("Secret123"), _hasher.Hash("Secret123"));
    }

    [Fact]
    public void Verify_accepts_the_original_password()
    {
        Assert.True(_hasher.Verify("Secret123", _hasher.Hash("Secret123")));
    }

    [Theory]
    [InlineData("secret123")]
    [InlineData("Secret1234")]
    [InlineData("")]
    public void Verify_rejects_any_other_password(string attempt)
    {
        Assert.False(_hasher.Verify(attempt, _hasher.Hash("Secret123")));
    }

    [Fact]
    public void Verify_honours_the_iteration_count_stored_in_the_hash()
    {
        var hash = new Pbkdf2PasswordHasher(iterations: 2_000).Hash("Secret123");

        Assert.True(_hasher.Verify("Secret123", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Secret123")]
    [InlineData("pbkdf2-sha256$1000$not-base64$also-not")]
    [InlineData("md5$1000$AAAA$AAAA")]
    [InlineData("pbkdf2-sha256$0$AAAA$AAAA")]
    public void Verify_rejects_malformed_hashes(string hash)
    {
        Assert.False(_hasher.Verify("Secret123", hash));
    }
}
