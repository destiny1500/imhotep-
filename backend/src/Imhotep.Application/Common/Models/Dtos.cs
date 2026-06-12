using Imhotep.Domain.Enums;

namespace Imhotep.Application.Common.Models;

public record UserDto(Guid Id, string Email, string FirstName, string LastName, UserRole Role);

public record AuthResultDto(string AccessToken, string RefreshToken, UserDto User);

public record TokenPairDto(string AccessToken, string RefreshToken);

public record PropertyDto(
    Guid Id,
    Guid OwnerId,
    Guid? ManagingAgencyId,
    string Label,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string PostalCode,
    string Country,
    PropertyType Type,
    PropertyStatus Status,
    decimal SurfaceM2,
    int Rooms,
    decimal RentAmount,
    decimal ChargesAmount);

public record LeaseDto(
    Guid Id,
    Guid PropertyId,
    Guid TenantId,
    string TenantName,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal RentAmount,
    decimal ChargesAmount,
    decimal DepositAmount,
    LeaseStatus Status);

public record MyLeaseDto(LeaseDto Lease, PropertyDto Property, string OwnerName, string? AgencyName);

public record PaymentDto(
    Guid Id,
    Guid LeaseId,
    decimal Amount,
    int PeriodYear,
    int PeriodMonth,
    DateTime PaidAtUtc,
    PaymentMethod Method,
    PaymentStatus Status,
    bool HasReceipt);

public record ReceiptDto(
    Guid Id,
    Guid PaymentId,
    Guid LeaseId,
    string Number,
    DateTime IssuedAtUtc,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal RentAmount,
    decimal ChargesAmount);

public record FileContentDto(string FileName, string ContentType, byte[] Content);

public record DocumentDto(
    Guid Id,
    Guid? PropertyId,
    Guid? LeaseId,
    DocumentType Type,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAt);

public record ConversationDto(
    Guid Id,
    string Subject,
    Guid? PropertyId,
    DateTime LastMessageAtUtc,
    IReadOnlyList<ParticipantDto> Participants);

public record ParticipantDto(Guid Id, string Name, UserRole Role);

public record MessageDto(Guid Id, Guid SenderId, string SenderName, string Body, DateTime SentAtUtc);

public record NotificationDto(
    Guid Id, NotificationType Type, string Title, string Body, bool IsRead, DateTime CreatedAt);

public record OwnerDashboardDto(
    int PropertiesCount,
    int ActiveLeases,
    decimal RentCollectedThisMonth,
    int LatePaymentsCount);

public record AgencyDashboardDto(
    int ManagedProperties,
    int OwnersCount,
    int TenantsCount,
    decimal OccupancyRate,
    int LatePaymentsCount,
    int MissingDocumentsCount,
    decimal RentCollectedThisMonth);
