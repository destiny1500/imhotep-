using FluentValidation;
using Imhotep.Application.Common.Behaviors;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Mappings;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using MediatR;

namespace Imhotep.Application.Features.Properties;

public record CreatePropertyCommand(
    string Label,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string PostalCode,
    string Country,
    PropertyType Type,
    decimal SurfaceM2,
    int Rooms,
    decimal RentAmount,
    decimal ChargesAmount) : IRequest<PropertyDto>, IAuditableCommand
{
    public string AuditAction => "property.create";
    public string? AuditEntityType => nameof(Property);
}

public class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AddressLine1).NotEmpty().MaximumLength(300);
        RuleFor(x => x.AddressLine2).MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.SurfaceM2).GreaterThan(0).LessThan(100_000);
        RuleFor(x => x.Rooms).GreaterThanOrEqualTo(0).LessThan(1000);
        RuleFor(x => x.RentAmount).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.ChargesAmount).GreaterThanOrEqualTo(0).LessThan(1_000_000);
    }
}

public class CreatePropertyCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IClock clock)
    : IRequestHandler<CreatePropertyCommand, PropertyDto>
{
    public async Task<PropertyDto> Handle(CreatePropertyCommand request, CancellationToken ct)
    {
        var property = new Property
        {
            OwnerId = currentUser.UserId!.Value,
            Label = request.Label.Trim(),
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = request.AddressLine2?.Trim(),
            City = request.City.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Country = request.Country.Trim(),
            Type = request.Type,
            SurfaceM2 = request.SurfaceM2,
            Rooms = request.Rooms,
            RentAmount = request.RentAmount,
            ChargesAmount = request.ChargesAmount,
            CreatedAt = clock.UtcNow
        };
        db.Properties.Add(property);
        await db.SaveChangesAsync(ct);
        return property.ToDto();
    }
}
