using FluentAssertions;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Features.Auth;
using Imhotep.Domain.Enums;
using Imhotep.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace Imhotep.UnitTests.Features.Auth;

public class LoginAndRefreshTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeClock _clock = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly Pbkdf2PasswordHasher _hasher = new();
    private readonly JwtTokenService _tokens;

    public LoginAndRefreshTests()
    {
        _tokens = new JwtTokenService(Options.Create(new JwtOptions
        {
            SigningKey = "unit-test-signing-key-0123456789abcdef0123456789abcdef",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        }), _clock);
    }

    private LoginCommandHandler LoginHandler() =>
        new(_db.Context, _hasher, _tokens, _currentUser, _clock);

    private RefreshTokenCommandHandler RefreshHandler() =>
        new(_db.Context, _tokens, _currentUser, _clock);

    private async Task SeedUser(string password = "S3cure!Passw0rd")
    {
        var user = Seed.User(UserRole.Tenant, "tenant@test.fr");
        user.PasswordHash = _hasher.Hash(password);
        _db.Context.Users.Add(user);
        await _db.Context.SaveChangesAsync();
    }

    [Fact]
    public async Task Login_returns_tokens_and_persists_hashed_refresh_token()
    {
        await SeedUser();
        var result = await LoginHandler().Handle(
            new LoginCommand("tenant@test.fr", "S3cure!Passw0rd"), CancellationToken.None);

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.User.Email.Should().Be("tenant@test.fr");
        // The raw refresh token must never be stored as-is.
        _db.Context.RefreshTokens.Should().ContainSingle()
            .Which.TokenHash.Should().NotBe(result.RefreshToken);
    }

    [Fact]
    public async Task Login_with_wrong_password_fails_with_generic_error()
    {
        await SeedUser();
        var act = () => LoginHandler().Handle(
            new LoginCommand("tenant@test.fr", "WrongPassword!1"), CancellationToken.None);
        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Account_locks_after_five_failed_attempts()
    {
        await SeedUser();
        for (var i = 0; i < 5; i++)
        {
            try
            {
                await LoginHandler().Handle(
                    new LoginCommand("tenant@test.fr", "WrongPassword!1"), CancellationToken.None);
            }
            catch (AuthenticationFailedException) { }
        }

        // Even the correct password is refused while locked out.
        var act = () => LoginHandler().Handle(
            new LoginCommand("tenant@test.fr", "S3cure!Passw0rd"), CancellationToken.None);
        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task Refresh_rotates_the_token()
    {
        await SeedUser();
        var login = await LoginHandler().Handle(
            new LoginCommand("tenant@test.fr", "S3cure!Passw0rd"), CancellationToken.None);

        var refreshed = await RefreshHandler().Handle(
            new RefreshTokenCommand(login.RefreshToken), CancellationToken.None);

        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);
        _db.Context.RefreshTokens.Count(t => t.RevokedAtUtc != null).Should().Be(1);
    }

    [Fact]
    public async Task Reusing_a_rotated_token_revokes_the_whole_chain()
    {
        await SeedUser();
        var login = await LoginHandler().Handle(
            new LoginCommand("tenant@test.fr", "S3cure!Passw0rd"), CancellationToken.None);
        await RefreshHandler().Handle(new RefreshTokenCommand(login.RefreshToken), CancellationToken.None);

        // Replay of the first (already rotated) token = theft signal.
        var act = () => RefreshHandler().Handle(
            new RefreshTokenCommand(login.RefreshToken), CancellationToken.None);
        await act.Should().ThrowAsync<AuthenticationFailedException>();

        _db.Context.RefreshTokens.Where(t => t.RevokedAtUtc == null).Should().BeEmpty(
            "all active tokens must be revoked when reuse is detected");
    }

    public void Dispose() => _db.Dispose();
}
