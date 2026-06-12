using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Imhotep.IntegrationTests;

/// <summary>
/// Owner invites a tenant who has no account: pending lease + invitation link,
/// the tenant creates their account through the link and lands on an active lease.
/// </summary>
public class InvitationFlowTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public InvitationFlowTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Invite_then_accept_attaches_the_lease_automatically()
    {
        var owner = await _factory.RegisterAndLoginAsync("owner-invite@test.fr", "Owner");

        var propertyResponse = await owner.PostAsJsonAsync("/api/properties", new
        {
            label = "Studio Bellecour",
            addressLine1 = "2 place Bellecour",
            city = "Lyon",
            postalCode = "69002",
            country = "France",
            type = "Apartment",
            surfaceM2 = 25,
            rooms = 1,
            rentAmount = 650,
            chargesAmount = 40
        });
        var property = await propertyResponse.Content.ReadFromJsonAsync<IdHolder>();

        // 1. Lease created for an e-mail with no account -> invitation link returned.
        var leaseResponse = await owner.PostAsJsonAsync("/api/leases", new
        {
            propertyId = property!.Id,
            tenantEmail = "invite-moi@test.fr",
            startDate = "2026-09-01",
            rentAmount = 650,
            chargesAmount = 40,
            depositAmount = 650
        });
        leaseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await leaseResponse.Content.ReadFromJsonAsync<CreateLeaseResult>();
        created!.InvitationUrl.Should().NotBeNullOrEmpty();
        var token = created.InvitationUrl!.Split('/').Last();

        // 2. The invited tenant cannot log in before accepting.
        var anonymous = _factory.CreateClient();
        var earlyLogin = await anonymous.PostAsJsonAsync("/api/auth/login", new
        {
            email = "invite-moi@test.fr",
            password = "S3cure!Passw0rd"
        });
        earlyLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 3. The invitation page can read the offer anonymously (token-gated).
        var info = await anonymous.GetAsync($"/api/invitations/{token}");
        info.StatusCode.Should().Be(HttpStatusCode.OK);
        (await info.Content.ReadAsStringAsync()).Should().Contain("Studio Bellecour");

        // 4. Accepting creates the account and signs the tenant in.
        var accept = await anonymous.PostAsJsonAsync($"/api/invitations/{token}/accept", new
        {
            firstName = "Léa",
            lastName = "Martin",
            password = "S3cure!Passw0rd"
        });
        accept.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await accept.Content.ReadFromJsonAsync<AuthResult>();
        auth!.AccessToken.Should().NotBeNullOrEmpty();

        // 5. The lease is attached and active for the new tenant.
        var tenant = _factory.CreateClient();
        tenant.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var myLease = await tenant.GetAsync("/api/leases/my");
        myLease.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await myLease.Content.ReadAsStringAsync();
        body.Should().Contain("Studio Bellecour").And.Contain("Active");

        // 6. The token is single-use.
        var replay = await anonymous.PostAsJsonAsync($"/api/invitations/{token}/accept", new
        {
            firstName = "Autre",
            lastName = "Personne",
            password = "S3cure!Passw0rd"
        });
        replay.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record IdHolder(Guid Id);
    private record CreateLeaseResult(IdHolder Lease, string? InvitationUrl);
    private record AuthResult(string AccessToken, string RefreshToken);
}
