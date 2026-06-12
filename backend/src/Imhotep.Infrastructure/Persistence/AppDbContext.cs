using Imhotep.Application.Common.Interfaces;
using Imhotep.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<ManagementContract> ManagementContracts => Set<ManagementContract>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<LeaseInvitation> LeaseInvitations => Set<LeaseInvitation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RentReceipt> RentReceipts => Set<RentReceipt>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
