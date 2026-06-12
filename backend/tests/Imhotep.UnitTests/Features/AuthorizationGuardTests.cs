using FluentAssertions;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Features.Payments;
using Imhotep.Application.Features.Properties;
using Imhotep.Application.Features.Receipts;
using Imhotep.Domain.Enums;
using Xunit;

namespace Imhotep.UnitTests.Features;

/// <summary>
/// Anti-IDOR guards: a user must never be able to read or mutate a resource
/// they do not own/manage, and the API must answer 404 (not 403) so the mere
/// existence of the resource is not leaked.
/// </summary>
public class AuthorizationGuardTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakeClock _clock = new();
    private readonly FakeCurrentUser _currentUser = new();

    private (Guid ownerId, Guid strangerId, Guid propertyId, Guid leaseId) SeedScenario()
    {
        var owner = Seed.User(UserRole.Owner, "owner@test.fr");
        var stranger = Seed.User(UserRole.Owner, "stranger@test.fr");
        var tenant = Seed.User(UserRole.Tenant, "tenant@test.fr");
        var property = Seed.Property(owner);
        var lease = Seed.Lease(property, tenant);
        _db.Context.AddRange(owner, stranger, tenant, property, lease);
        _db.Context.SaveChanges();
        return (owner.Id, stranger.Id, property.Id, lease.Id);
    }

    [Fact]
    public async Task Stranger_cannot_read_someone_elses_property()
    {
        var (_, strangerId, propertyId, _) = SeedScenario();
        _currentUser.UserId = strangerId;

        var handler = new GetPropertyQueryHandler(_db.Context, _currentUser);
        var act = () => handler.Handle(new GetPropertyQuery(propertyId), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Owner_can_read_their_property()
    {
        var (ownerId, _, propertyId, _) = SeedScenario();
        _currentUser.UserId = ownerId;

        var handler = new GetPropertyQueryHandler(_db.Context, _currentUser);
        var dto = await handler.Handle(new GetPropertyQuery(propertyId), CancellationToken.None);
        dto.Id.Should().Be(propertyId);
    }

    [Fact]
    public async Task Stranger_cannot_record_a_payment_on_someone_elses_lease()
    {
        var (_, strangerId, _, leaseId) = SeedScenario();
        _currentUser.UserId = strangerId;

        var handler = new RecordPaymentCommandHandler(_db.Context, _currentUser, _clock);
        var act = () => handler.Handle(
            new RecordPaymentCommand(leaseId, 850, 2026, 6, _clock.UtcNow, PaymentMethod.BankTransfer),
            CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Owner_records_payment_then_cannot_issue_two_receipts_for_it()
    {
        var (ownerId, _, _, leaseId) = SeedScenario();
        _currentUser.UserId = ownerId;

        var payment = await new RecordPaymentCommandHandler(_db.Context, _currentUser, _clock)
            .Handle(new RecordPaymentCommand(leaseId, 850, 2026, 6, _clock.UtcNow, PaymentMethod.BankTransfer),
                CancellationToken.None);

        var receiptHandler = new GenerateReceiptCommandHandler(_db.Context, _currentUser, _clock);
        var receipt = await receiptHandler.Handle(new GenerateReceiptCommand(payment.Id), CancellationToken.None);
        receipt.Number.Should().MatchRegex(@"^Q-\d{4}-\d{6}$");

        var act = () => receiptHandler.Handle(new GenerateReceiptCommand(payment.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Duplicate_payment_for_same_period_is_rejected()
    {
        var (ownerId, _, _, leaseId) = SeedScenario();
        _currentUser.UserId = ownerId;
        var handler = new RecordPaymentCommandHandler(_db.Context, _currentUser, _clock);
        var command = new RecordPaymentCommand(leaseId, 850, 2026, 6, _clock.UtcNow, PaymentMethod.BankTransfer);

        await handler.Handle(command, CancellationToken.None);
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>();
    }

    public void Dispose() => _db.Dispose();
}
