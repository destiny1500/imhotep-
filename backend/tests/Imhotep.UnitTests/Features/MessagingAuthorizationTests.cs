using FluentAssertions;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Features.Messaging;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using Xunit;

namespace Imhotep.UnitTests.Features;

/// <summary>
/// Messaging is restricted to people a property links together: an owner may only
/// write to their tenants or their managing agency, and a tenant may only write to
/// their owner or that agency. Unrelated users get a 403.
/// </summary>
public class MessagingAuthorizationTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeClock _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    private StartConversationCommandHandler Handler() => new(_db.Context, _currentUser, _clock);

    private static StartConversationCommand To(Guid participant) =>
        new(participant, null, "Sujet", "Bonjour");

    private async Task<Guid> StartAs(UserRole role, Guid sender, Guid participant)
    {
        _currentUser.UserId = sender;
        _currentUser.Role = role;
        return await Handler().Handle(To(participant), CancellationToken.None);
    }

    [Fact]
    public async Task Owner_can_message_their_tenant()
    {
        var owner = Seed.User(UserRole.Owner, "owner@test.fr");
        var tenant = Seed.User(UserRole.Tenant, "tenant@test.fr");
        var property = Seed.Property(owner);
        var lease = Seed.Lease(property, tenant);
        _db.Context.AddRange(owner, tenant, property, lease);
        await _db.Context.SaveChangesAsync();

        var id = await StartAs(UserRole.Owner, owner.Id, tenant.Id);
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Tenant_can_message_their_owner()
    {
        var owner = Seed.User(UserRole.Owner, "owner@test.fr");
        var tenant = Seed.User(UserRole.Tenant, "tenant@test.fr");
        var property = Seed.Property(owner);
        var lease = Seed.Lease(property, tenant);
        _db.Context.AddRange(owner, tenant, property, lease);
        await _db.Context.SaveChangesAsync();

        var id = await StartAs(UserRole.Tenant, tenant.Id, owner.Id);
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Owner_can_message_their_managing_agency()
    {
        var owner = Seed.User(UserRole.Owner, "owner@test.fr");
        var agency = Seed.User(UserRole.Agency, "agency@test.fr");
        var property = Seed.Property(owner);
        property.ManagingAgencyId = agency.Id;
        _db.Context.AddRange(owner, agency, property);
        await _db.Context.SaveChangesAsync();

        var id = await StartAs(UserRole.Owner, owner.Id, agency.Id);
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Tenant_can_message_the_managing_agency()
    {
        var owner = Seed.User(UserRole.Owner, "owner@test.fr");
        var agency = Seed.User(UserRole.Agency, "agency@test.fr");
        var tenant = Seed.User(UserRole.Tenant, "tenant@test.fr");
        var property = Seed.Property(owner);
        property.ManagingAgencyId = agency.Id;
        var lease = Seed.Lease(property, tenant);
        _db.Context.AddRange(owner, agency, tenant, property, lease);
        await _db.Context.SaveChangesAsync();

        var id = await StartAs(UserRole.Tenant, tenant.Id, agency.Id);
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Tenant_cannot_message_another_tenant()
    {
        var owner = Seed.User(UserRole.Owner, "owner@test.fr");
        var tenant1 = Seed.User(UserRole.Tenant, "t1@test.fr");
        var tenant2 = Seed.User(UserRole.Tenant, "t2@test.fr");
        var property = Seed.Property(owner);
        var lease1 = Seed.Lease(property, tenant1);
        var lease2 = Seed.Lease(property, tenant2);
        _db.Context.AddRange(owner, tenant1, tenant2, property, lease1, lease2);
        await _db.Context.SaveChangesAsync();

        var act = () => StartAs(UserRole.Tenant, tenant1.Id, tenant2.Id);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Owner_cannot_message_an_unrelated_tenant()
    {
        var owner = Seed.User(UserRole.Owner, "owner@test.fr");
        var otherOwner = Seed.User(UserRole.Owner, "other@test.fr");
        var tenant = Seed.User(UserRole.Tenant, "tenant@test.fr");
        var otherProperty = Seed.Property(otherOwner);
        var lease = Seed.Lease(otherProperty, tenant);
        _db.Context.AddRange(owner, otherOwner, tenant, otherProperty, lease);
        await _db.Context.SaveChangesAsync();

        var act = () => StartAs(UserRole.Owner, owner.Id, tenant.Id);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Owner_cannot_message_a_former_tenant_whose_lease_ended()
    {
        var owner = Seed.User(UserRole.Owner, "owner@test.fr");
        var tenant = Seed.User(UserRole.Tenant, "tenant@test.fr");
        var property = Seed.Property(owner);
        var lease = Seed.Lease(property, tenant);
        lease.Status = LeaseStatus.Terminated;
        _db.Context.AddRange(owner, tenant, property, lease);
        await _db.Context.SaveChangesAsync();

        var act = () => StartAs(UserRole.Owner, owner.Id, tenant.Id);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Admin_may_message_anyone()
    {
        var admin = Seed.User(UserRole.Admin, "admin@test.fr");
        var tenant = Seed.User(UserRole.Tenant, "tenant@test.fr");
        _db.Context.AddRange(admin, tenant);
        await _db.Context.SaveChangesAsync();

        var id = await StartAs(UserRole.Admin, admin.Id, tenant.Id);
        id.Should().NotBeEmpty();
    }

    public void Dispose() => _db.Dispose();
}
