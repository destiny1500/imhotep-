using AutoMapper;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Mappings;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using Imhotep.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.UnitTests;

public sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
}

public sealed class FakeCurrentUser : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public UserRole? Role { get; set; }
    public string? IpAddress { get; set; } = "127.0.0.1";
    public bool IsAuthenticated => UserId.HasValue;
}

/// <summary>SQLite in-memory database that lives as long as the helper (connection kept open).</summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public AppDbContext Context { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

public static class TestMapper
{
    public static IMapper Create() =>
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
}

public static class Seed
{
    public static User User(UserRole role, string email = "user@test.fr") => new()
    {
        Email = email,
        PasswordHash = "irrelevant",
        FirstName = "Jean",
        LastName = "Dupont",
        Role = role,
        CreatedAt = DateTime.UtcNow
    };

    public static Property Property(User owner) => new()
    {
        OwnerId = owner.Id,
        Owner = owner,
        Label = "T2 République",
        AddressLine1 = "10 rue de la République",
        City = "Lyon",
        PostalCode = "69001",
        Country = "France",
        Type = PropertyType.Apartment,
        SurfaceM2 = 45,
        Rooms = 2,
        RentAmount = 800,
        ChargesAmount = 50,
        CreatedAt = DateTime.UtcNow
    };

    public static Lease Lease(Property property, User tenant) => new()
    {
        PropertyId = property.Id,
        Property = property,
        TenantId = tenant.Id,
        Tenant = tenant,
        StartDate = new DateOnly(2026, 1, 1),
        RentAmount = 800,
        ChargesAmount = 50,
        DepositAmount = 800,
        Status = LeaseStatus.Active,
        CreatedAt = DateTime.UtcNow
    };
}
