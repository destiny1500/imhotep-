using Imhotep.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imhotep.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(u => u.Email).HasMaxLength(254).IsRequired();
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        b.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        b.Ignore(u => u.FullName);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        b.HasIndex(t => t.TokenHash).IsUnique();
        b.HasOne(t => t.User).WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> b)
    {
        b.Property(p => p.Label).HasMaxLength(200).IsRequired();
        b.Property(p => p.AddressLine1).HasMaxLength(300).IsRequired();
        b.Property(p => p.AddressLine2).HasMaxLength(300);
        b.Property(p => p.City).HasMaxLength(120).IsRequired();
        b.Property(p => p.PostalCode).HasMaxLength(20).IsRequired();
        b.Property(p => p.Country).HasMaxLength(100).IsRequired();
        b.Property(p => p.SurfaceM2).HasPrecision(10, 2);
        b.Property(p => p.RentAmount).HasPrecision(12, 2);
        b.Property(p => p.ChargesAmount).HasPrecision(12, 2);
        b.HasOne(p => p.Owner).WithMany()
            .HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.ManagingAgency).WithMany()
            .HasForeignKey(p => p.ManagingAgencyId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(p => p.OwnerId);
        b.HasIndex(p => p.ManagingAgencyId);
    }
}

public class ManagementContractConfiguration : IEntityTypeConfiguration<ManagementContract>
{
    public void Configure(EntityTypeBuilder<ManagementContract> b)
    {
        b.Property(c => c.FeePercent).HasPrecision(5, 2);
        b.HasOne(c => c.Property).WithMany()
            .HasForeignKey(c => c.PropertyId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.Owner).WithMany()
            .HasForeignKey(c => c.OwnerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(c => c.Agency).WithMany()
            .HasForeignKey(c => c.AgencyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> b)
    {
        b.Property(l => l.RentAmount).HasPrecision(12, 2);
        b.Property(l => l.ChargesAmount).HasPrecision(12, 2);
        b.Property(l => l.DepositAmount).HasPrecision(12, 2);
        b.HasOne(l => l.Property).WithMany(p => p.Leases)
            .HasForeignKey(l => l.PropertyId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(l => l.Tenant).WithMany()
            .HasForeignKey(l => l.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(l => l.TenantId);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.Property(p => p.Amount).HasPrecision(12, 2);
        b.HasOne(p => p.Lease).WithMany(l => l.Payments)
            .HasForeignKey(p => p.LeaseId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.RecordedBy).WithMany()
            .HasForeignKey(p => p.RecordedById).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(p => new { p.LeaseId, p.PeriodYear, p.PeriodMonth });
    }
}

public class RentReceiptConfiguration : IEntityTypeConfiguration<RentReceipt>
{
    public void Configure(EntityTypeBuilder<RentReceipt> b)
    {
        b.Property(r => r.Number).HasMaxLength(30).IsRequired();
        b.HasIndex(r => r.Number).IsUnique();
        b.Property(r => r.RentAmount).HasPrecision(12, 2);
        b.Property(r => r.ChargesAmount).HasPrecision(12, 2);
        b.HasOne(r => r.Payment).WithOne(p => p.Receipt)
            .HasForeignKey<RentReceipt>(r => r.PaymentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(r => r.Lease).WithMany()
            .HasForeignKey(r => r.LeaseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(r => r.IssuedBy).WithMany()
            .HasForeignKey(r => r.IssuedById).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        b.Property(d => d.ContentType).HasMaxLength(150).IsRequired();
        b.Property(d => d.StoragePath).HasMaxLength(500).IsRequired();
        b.HasOne(d => d.Property).WithMany(p => p.Documents)
            .HasForeignKey(d => d.PropertyId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(d => d.Lease).WithMany(l => l.Documents)
            .HasForeignKey(d => d.LeaseId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(d => d.UploadedBy).WithMany()
            .HasForeignKey(d => d.UploadedById).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.Property(c => c.Subject).HasMaxLength(200).IsRequired();
        b.HasOne(c => c.Property).WithMany()
            .HasForeignKey(c => c.PropertyId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> b)
    {
        b.HasKey(p => new { p.ConversationId, p.UserId });
        b.HasOne(p => p.Conversation).WithMany(c => c.Participants)
            .HasForeignKey(p => p.ConversationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.User).WithMany()
            .HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.Property(m => m.Body).HasMaxLength(10_000).IsRequired();
        b.HasOne(m => m.Conversation).WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(m => m.Sender).WithMany()
            .HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(m => m.ConversationId);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.Property(n => n.Title).HasMaxLength(200).IsRequired();
        b.Property(n => n.Body).HasMaxLength(1000).IsRequired();
        b.HasOne(n => n.User).WithMany()
            .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(n => new { n.UserId, n.IsRead });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(a => a.Action).HasMaxLength(100).IsRequired();
        b.Property(a => a.EntityType).HasMaxLength(100);
        b.Property(a => a.EntityId).HasMaxLength(64);
        b.Property(a => a.IpAddress).HasMaxLength(45);
        b.HasIndex(a => a.TimestampUtc);
        b.HasIndex(a => a.UserId);
    }
}
