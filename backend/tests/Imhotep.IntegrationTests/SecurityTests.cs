using FluentAssertions;
using Imhotep.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Imhotep.IntegrationTests;

public class SecurityTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public SecurityTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_request_to_protected_endpoint_returns_401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/properties");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tenant_cannot_create_a_property_403()
    {
        var client = await _factory.RegisterAndLoginAsync("tenant-sec@test.fr", "Tenant");
        var response = await client.PostAsJsonAsync("/api/properties", new
        {
            label = "Tentative",
            addressLine1 = "1 rue Interdite",
            city = "Paris",
            postalCode = "75001",
            country = "France",
            type = "Apartment",
            surfaceM2 = 30,
            rooms = 1,
            rentAmount = 900,
            chargesAmount = 50
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Tenant_cannot_access_agency_dashboard_403()
    {
        var client = await _factory.RegisterAndLoginAsync("tenant-dash@test.fr", "Tenant");
        var response = await client.GetAsync("/api/dashboard/agency");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_role_cannot_be_self_registered()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "evil@test.fr",
            password = "S3cure!Passw0rd",
            firstName = "Evil",
            lastName = "Admin",
            role = "Admin"
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Owner_cannot_read_another_owners_property_404()
    {
        var ownerA = await _factory.RegisterAndLoginAsync("owner-a@test.fr", "Owner");
        var created = await ownerA.PostAsJsonAsync("/api/properties", new
        {
            label = "Bien de A",
            addressLine1 = "1 rue de A",
            city = "Lyon",
            postalCode = "69001",
            country = "France",
            type = "Apartment",
            surfaceM2 = 50,
            rooms = 2,
            rentAmount = 800,
            chargesAmount = 50
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var property = await created.Content.ReadFromJsonAsync<PropertyResponse>();

        var ownerB = await _factory.RegisterAndLoginAsync("owner-b@test.fr", "Owner");
        var response = await ownerB.GetAsync($"/api/properties/{property!.Id}");
        // 404 rather than 403: existence must not leak (anti-IDOR).
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_token_is_rejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "forged.invalid.token");
        var response = await client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_of_a_user_who_no_longer_exists_is_rejected_401()
    {
        // A valid signed JWT must stop working the moment its user disappears
        // (account deleted, database reset) — 401, never a 500 on FK violations.
        var client = await _factory.RegisterAndLoginAsync("ghost@test.fr", "Owner");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ghost = await db.Users.SingleAsync(u => u.Email == "ghost@test.fr");
            db.RefreshTokens.RemoveRange(db.RefreshTokens.Where(t => t.UserId == ghost.Id));
            db.Users.Remove(ghost);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Security_headers_are_present_on_responses()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/notifications"); // 401, headers still set
        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("Content-Security-Policy");
    }

    private record PropertyResponse(Guid Id);
}
