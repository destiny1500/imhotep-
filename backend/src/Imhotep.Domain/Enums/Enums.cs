namespace Imhotep.Domain.Enums;

public enum UserRole
{
    Tenant = 1,
    Owner = 2,
    Agency = 3,
    Admin = 4
}

public enum PropertyType
{
    Apartment = 1,
    House = 2,
    Commercial = 3,
    Parking = 4,
    Other = 5
}

public enum PropertyStatus
{
    Available = 1,
    Rented = 2,
    UnderMaintenance = 3,
    Archived = 4
}

public enum LeaseStatus
{
    Active = 1,
    Terminated = 2,
    Pending = 3
}

public enum PaymentMethod
{
    BankTransfer = 1,
    Card = 2,
    Cash = 3,
    Check = 4
}

public enum PaymentStatus
{
    Completed = 1,
    Pending = 2,
    Failed = 3
}

public enum DocumentType
{
    LeaseContract = 1,
    RentReceipt = 2,
    Diagnostic = 3,
    Insurance = 4,
    Inventory = 5,
    Other = 6
}

public enum ManagementContractStatus
{
    Active = 1,
    Terminated = 2
}

public enum NotificationType
{
    PaymentRecorded = 1,
    DocumentAdded = 2,
    RentReminder = 3,
    ReceiptIssued = 4,
    MessageReceived = 5,
    LeaseCreated = 6
}
