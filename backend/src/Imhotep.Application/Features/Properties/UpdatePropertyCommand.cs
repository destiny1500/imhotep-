using FluentValidation;
using Imhotep.Application.Common.Behaviors;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Mappings;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Properties;

public record UpdatePropertyCommand(
    Guid Id,
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
    decimal ChargesAmount) : IRequest<PropertyDto>, IAuditableCommand
{
    public string AuditAction => "property.update";
    public string? AuditEntityType => nameof(Property);
    public string? AuditEntityId => Id.ToString();
}

public class UpdatePropertyCommandValidator : AbstractValidator<UpdatePropertyCommand>
{
    public UpdatePropertyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.SurfaceM2).GreaterThan(0).LessThan(100_000);
        RuleFor(x => x.Rooms).GreaterThanOrEqualTo(0).LessThan(1000);
        RuleFor(x => x.RentAmount).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.ChargesAmount).GreaterThanOrEqualTo(0).LessThan(1_000_000);
    }
}

public class UpdatePropertyCommandHandler(IAppDbContext db, ICurrentUserService currentUser, IClock clock)
    : IRequestHandler<UpdatePropertyCommand, PropertyDto>
{
    public async Task<PropertyDto> Handle(UpdatePropertyCommand request, CancellationToken ct)
    {
        var property = await db.Properties.FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Property), request.Id);

        // Anti-IDOR: only the owner or the managing agency can modify the property.
        if (!property.IsManagedBy(currentUser.UserId!.Value))
            throw new NotFoundException(nameof(Property), request.Id);

        property.Label = request.Label.Trim();
        property.AddressLine1 = request.AddressLine1.Trim();
        property.AddressLine2 = request.AddressLine2?.Trim();
        property.City = request.City.Trim();
        property.PostalCode = request.PostalCode.Trim();
        property.Country = request.Country.Trim();
        property.Type = request.Type;
        property.Status = request.Status;
        property.SurfaceM2 = request.SurfaceM2;
        property.Rooms = request.Rooms;
        property.RentAmount = request.RentAmount;
        property.ChargesAmount = request.ChargesAmount;
        property.UpdatedAt = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        return property.ToDto();
    }
}
