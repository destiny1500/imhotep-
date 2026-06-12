using FluentAssertions;
using Imhotep.Infrastructure.Identity;
using Xunit;

namespace Imhotep.UnitTests.Identity;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_then_verify_succeeds()
    {
        var hash = _hasher.Hash("S3cure!Passw0rd#2026");
        _hasher.Verify("S3cure!Passw0rd#2026", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_fails_for_wrong_password()
    {
        var hash = _hasher.Hash("S3cure!Passw0rd#2026");
        _hasher.Verify("WrongPassword!1", hash).Should().BeFalse();
    }

    [Fact]
    public void Hashing_same_password_twice_produces_different_hashes()
    {
        // Random salt per hash.
        _hasher.Hash("S3cure!Passw0rd#2026").Should().NotBe(_hasher.Hash("S3cure!Passw0rd#2026"));
    }

    [Fact]
    public void Hash_embeds_owasp_level_iteration_count()
    {
        var iterations = int.Parse(_hasher.Hash("x").Split('.')[0]);
        iterations.Should().BeGreaterThanOrEqualTo(600_000);
    }

    [Fact]
    public void Verify_returns_false_for_malformed_hash()
    {
        _hasher.Verify("password", "not-a-valid-hash").Should().BeFalse();
        _hasher.Verify("password", "12345.###.###").Should().BeFalse();
    }
}
