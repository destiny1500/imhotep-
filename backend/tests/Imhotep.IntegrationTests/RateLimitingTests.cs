using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Imhotep.IntegrationTests;

/// <summary>Dedicated factory with a deliberately low auth limit.</summary>
public class LowRateLimitFactory : TestWebAppFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimiting:AuthPermitLimit", "3");
    }
}

public class RateLimitingTests : IClassFixture<LowRateLimitFactory>
{
    private readonly LowRateLimitFactory _factory;

    public RateLimitingTests(LowRateLimitFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_endpoint_is_rate_limited_against_bruteforce()
    {
        var client = _factory.CreateClient();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "bruteforce@test.fr",
                password = $"Wrong!Passw0rd{i}"
            });
            statuses.Add(response.StatusCode);
        }

        statuses.Should().Contain(HttpStatusCode.TooManyRequests,
            "repeated login attempts from one IP must be throttled");
    }
}
