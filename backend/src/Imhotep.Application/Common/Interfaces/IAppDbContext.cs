using Imhotep.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Property> Properties { get; }
    DbSet<ManagementContract> ManagementContracts { get; }
    DbSet<Lease> Leases { get; }
    DbSet<LeaseInvitation> LeaseInvitations { get; }
    DbSet<Payment> Payments { get; }
    DbSet<RentReceipt> RentReceipts { get; }
    DbSet<Document> Documents { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
