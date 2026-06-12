using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Imhotep.IntegrationTests;

/// <summary>
/// End-to-end business flow: owner creates a property, leases it to a tenant,
/// records a payment, issues the quittance; the tenant sees and downloads it.
/// </summary>
public class RentalFlowTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public RentalFlowTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Full_rental_lifecycle()
    {
        var owner = await _factory.RegisterAndLoginAsync("owner-flow@test.fr", "Owner");
        var tenant = await _factory.RegisterAndLoginAsync("tenant-flow@test.fr", "Tenant");

        // 1. Owner creates a property.
        var propertyResponse = await owner.PostAsJsonAsync("/api/properties", new
        {
            label = "T3 Croix-Rousse",
            addressLine1 = "5 rue des Tables Claudiennes",
            city = "Lyon",
            postalCode = "69001",
            country = "France",
            type = "Apartment",
            surfaceM2 = 65,
            rooms = 3,
            rentAmount = 950,
            chargesAmount = 80
        });
        propertyResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var property = await propertyResponse.Content.ReadFromJsonAsync<IdHolder>();

        // 2. Owner creates a lease for the tenant.
        var leaseResponse = await owner.PostAsJsonAsync("/api/leases", new
        {
            propertyId = property!.Id,
            tenantEmail = "tenant-flow@test.fr",
            startDate = "2026-01-01",
            rentAmount = 950,
            chargesAmount = 80,
            depositAmount = 950
        });
        leaseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var leaseResult = await leaseResponse.Content.ReadFromJsonAsync<CreateLeaseResult>();
        leaseResult!.InvitationUrl.Should().BeNull("the tenant already has an account");
        var lease = leaseResult.Lease;

        // 3. Tenant sees their housing info.
        var myLease = await tenant.GetAsync("/api/leases/my");
        myLease.StatusCode.Should().Be(HttpStatusCode.OK);
        (await myLease.Content.ReadAsStringAsync()).Should().Contain("Croix-Rousse");

        // 4. Owner records the June payment.
        var paymentResponse = await owner.PostAsJsonAsync("/api/payments", new
        {
            leaseId = lease!.Id,
            amount = 1030,
            periodYear = 2026,
            periodMonth = 6,
            paidAt = "2026-06-03T10:00:00Z",
            method = "BankTransfer"
        });
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await paymentResponse.Content.ReadFromJsonAsync<IdHolder>();

        // 5. Owner generates the quittance.
        var receiptResponse = await owner.PostAsJsonAsync("/api/receipts/generate", new
        {
            paymentId = payment!.Id
        });
        receiptResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var receipt = await receiptResponse.Content.ReadFromJsonAsync<IdHolder>();

        // 6. Tenant lists their receipts and downloads the document.
        var myReceipts = await tenant.GetFromJsonAsync<List<IdHolder>>("/api/receipts/my");
        myReceipts.Should().ContainSingle(r => r.Id == receipt!.Id);

        var download = await tenant.GetAsync($"/api/receipts/{receipt!.Id}/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await download.Content.ReadAsStringAsync();
        html.Should().Contain("Quittance de loyer");

        // 7. Tenant received notifications along the way.
        var notifications = await tenant.GetAsync("/api/notifications");
        var body = await notifications.Content.ReadAsStringAsync();
        body.Should().Contain("Quittance disponible");

        // 8. A different tenant cannot download that receipt (anti-IDOR -> 404).
        var otherTenant = await _factory.RegisterAndLoginAsync("intruder@test.fr", "Tenant");
        var forbidden = await otherTenant.GetAsync($"/api/receipts/{receipt.Id}/download");
        forbidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record IdHolder(Guid Id);
    private record CreateLeaseResult(IdHolder Lease, string? InvitationUrl);
}
