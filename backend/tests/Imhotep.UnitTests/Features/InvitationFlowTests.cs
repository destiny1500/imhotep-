using FluentAssertions;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Features.Invitations;
using Imhotep.Application.Features.Leases;
using Imhotep.Domain.Enums;
using Imhotep.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace Imhotep.UnitTests.Features;

public class InvitationFlowTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeClock _clock = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly JwtTokenService _tokens;

    public InvitationFlowTests()
    {
        _tokens = new JwtTokenService(Options.Create(new JwtOptions
        {
            SigningKey = "unit-test-signing-key-0123456789abcdef0123456789abcdef"
        }), _clock);
    }

    private CreateLeaseCommandHandler CreateLeaseHandler() =>
        new(_db.Context, _currentUser, _clock, _tokens, new FakeLinkBuilder(), _emailSender);

    private AcceptInvitationCommandHandler AcceptHandler() =>
        new(_db.Context, new Pbkdf2PasswordHasher(), _tokens, _currentUser, _clock);

    private Guid SeedOwnerWithProperty(out Guid propertyId)
    {
        var owner = Seed.User(UserRole.Owner, "owner@test.fr");
        var property = Seed.Property(owner);
        _db.Context.AddRange(owner, property);
        _db.Context.SaveChanges();
        propertyId = property.Id;
        return owner.Id;
    }

    private static CreateLeaseCommand LeaseCommand(Guid propertyId, string email) =>
        new(propertyId, email, new DateOnly(2026, 7, 1), null, 950, 80, 950);

    [Fact]
    public async Task Unknown_email_creates_pending_lease_with_invitation()
    {
        _currentUser.UserId = SeedOwnerWithProperty(out var propertyId);

        var result = await CreateLeaseHandler().Handle(
            LeaseCommand(propertyId, "nouveau@test.fr"), CancellationToken.None);

        result.InvitationUrl.Should().NotBeNullOrEmpty();
        result.Lease.Status.Should().Be(LeaseStatus.Pending);
        result.Lease.TenantName.Should().Be("nouveau@test.fr", "name falls back to e-mail until accepted");

        var tenant = _db.Context.Users.Single(u => u.Email == "nouveau@test.fr");
        tenant.IsActive.Should().BeFalse();
        tenant.PasswordHash.Should().BeEmpty("the placeholder account must be unusable for login");

        var invitation = _db.Context.LeaseInvitations.Single();
        invitation.TokenHash.Should().NotBe(GetRawTokenFromUrl(result.InvitationUrl!),
            "only the token hash is persisted");
        _emailSender.Sent.Should().ContainSingle().Which.To.Should().Be("nouveau@test.fr");
    }

    [Fact]
    public async Task Existing_tenant_email_creates_active_lease_without_invitation()
    {
        _currentUser.UserId = SeedOwnerWithProperty(out var propertyId);
        var tenant = Seed.User(UserRole.Tenant, "tenant@test.fr");
        _db.Context.Add(tenant);
        _db.Context.SaveChanges();

        var result = await CreateLeaseHandler().Handle(
            LeaseCommand(propertyId, "tenant@test.fr"), CancellationToken.None);

        result.InvitationUrl.Should().BeNull();
        result.Lease.Status.Should().Be(LeaseStatus.Active);
        _emailSender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Email_of_non_tenant_account_is_rejected()
    {
        _currentUser.UserId = SeedOwnerWithProperty(out var propertyId);

        // The owner uses their own (Owner-role) e-mail.
        var act = () => CreateLeaseHandler().Handle(
            LeaseCommand(propertyId, "owner@test.fr"), CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Accepting_invitation_activates_account_and_lease_and_signs_in()
    {
        var ownerId = SeedOwnerWithProperty(out var propertyId);
        _currentUser.UserId = ownerId;
        var created = await CreateLeaseHandler().Handle(
            LeaseCommand(propertyId, "nouveau@test.fr"), CancellationToken.None);
        var rawToken = GetRawTokenFromUrl(created.InvitationUrl!);

        var auth = await AcceptHandler().Handle(
            new AcceptInvitationCommand(rawToken, "Léa", "Martin", "S3cure!Passw0rd"),
            CancellationToken.None);

        auth.AccessToken.Should().NotBeNullOrEmpty();
        auth.User.Email.Should().Be("nouveau@test.fr");

        var tenant = _db.Context.Users.Single(u => u.Email == "nouveau@test.fr");
        tenant.IsActive.Should().BeTrue();
        tenant.FirstName.Should().Be("Léa");

        _db.Context.Leases.Single().Status.Should().Be(LeaseStatus.Active);
        _db.Context.Properties.Single().Status.Should().Be(PropertyStatus.Rented);
        _db.Context.Notifications.Should().Contain(n => n.UserId == ownerId,
            "the inviter is notified of the acceptance");
    }

    [Fact]
    public async Task Invitation_token_cannot_be_used_twice()
    {
        _currentUser.UserId = SeedOwnerWithProperty(out var propertyId);
        var created = await CreateLeaseHandler().Handle(
            LeaseCommand(propertyId, "nouveau@test.fr"), CancellationToken.None);
        var rawToken = GetRawTokenFromUrl(created.InvitationUrl!);

        await AcceptHandler().Handle(
            new AcceptInvitationCommand(rawToken, "Léa", "Martin", "S3cure!Passw0rd"),
            CancellationToken.None);

        var act = () => AcceptHandler().Handle(
            new AcceptInvitationCommand(rawToken, "Autre", "Personne", "S3cure!Passw0rd"),
            CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Expired_invitation_is_rejected()
    {
        _currentUser.UserId = SeedOwnerWithProperty(out var propertyId);
        var created = await CreateLeaseHandler().Handle(
            LeaseCommand(propertyId, "nouveau@test.fr"), CancellationToken.None);
        var rawToken = GetRawTokenFromUrl(created.InvitationUrl!);

        _clock.UtcNow = _clock.UtcNow.AddDays(15); // past the 14-day lifetime

        var act = () => AcceptHandler().Handle(
            new AcceptInvitationCommand(rawToken, "Léa", "Martin", "S3cure!Passw0rd"),
            CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static string GetRawTokenFromUrl(string url) => url.Split('/').Last();

    public void Dispose() => _db.Dispose();
}
