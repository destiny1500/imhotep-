using FluentAssertions;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Features.Auth;
using Imhotep.Domain.Enums;
using Imhotep.Infrastructure.Identity;
using Xunit;

namespace Imhotep.UnitTests.Features.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand Valid(string password = "S3cure!Passw0rd") =>
        new("user@test.fr", password, "Jean", "Dupont", UserRole.Tenant);

    [Fact]
    public void Accepts_valid_command() =>
        _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("short!A1")]               // too short
    [InlineData("alllowercase!1234")]      // no uppercase
    [InlineData("ALLUPPERCASE!1234")]      // no lowercase
    [InlineData("NoDigitsHere!!!!")]       // no digit
    [InlineData("NoSymbolsHere1234")]      // no symbol
    public void Rejects_weak_passwords(string password) =>
        _validator.Validate(Valid(password)).IsValid.Should().BeFalse();

    [Fact]
    public void Rejects_admin_self_registration()
    {
        var command = Valid() with { Role = UserRole.Admin };
        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_invalid_email()
    {
        var command = Valid() with { Email = "not-an-email" };
        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}

public class RegisterCommandHandlerTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(_db.Context, new Pbkdf2PasswordHasher(), new FakeClock());
    }

    [Fact]
    public async Task Creates_user_with_hashed_password_and_normalized_email()
    {
        var command = new RegisterCommand("User@Test.FR", "S3cure!Passw0rd", "Jean", "Dupont", UserRole.Owner);
        var id = await _handler.Handle(command, CancellationToken.None);

        var user = _db.Context.Users.Single(u => u.Id == id);
        user.Email.Should().Be("user@test.fr");
        user.PasswordHash.Should().NotContain("S3cure", "the password must never be stored in clear text");
        user.Role.Should().Be(UserRole.Owner);
    }

    [Fact]
    public async Task Rejects_duplicate_email()
    {
        var command = new RegisterCommand("user@test.fr", "S3cure!Passw0rd", "Jean", "Dupont", UserRole.Tenant);
        await _handler.Handle(command, CancellationToken.None);

        var act = () => _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    public void Dispose() => _db.Dispose();
}
