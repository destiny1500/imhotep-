using Imhotep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Imhotep.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (FastEndpoints, JWT, rate limiting, middleware)
/// against an in-memory SQLite database instead of PostgreSQL.
/// </summary>
public class TestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-0123456789abcdef0123456789");
        builder.UseSetting("DocumentStorage:EncryptionKeyBase64",
            Convert.ToBase64String(new byte[32]));
        builder.UseSetting("DocumentStorage:RootPath",
            Path.Combine(Path.GetTempPath(), $"imhotep-tests-{Guid.NewGuid():N}"));
        builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "10000");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

public static class ClientExtensions
{
    public record AuthResult(string AccessToken, string RefreshToken);

    public static async Task<HttpClient> RegisterAndLoginAsync(
        this TestWebAppFactory factory, string email, string role, string password = "S3cure!Passw0rd")
    {
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "User",
            role
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthResult>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }
}
