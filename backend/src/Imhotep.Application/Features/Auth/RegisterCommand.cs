using FluentValidation;
using Imhotep.Application.Common.Behaviors;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Validation;
using Imhotep.Domain.Entities;
using Imhotep.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Auth;

public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role) : IRequest<Guid>, IAuditableCommand
{
    public string AuditAction => "auth.register";
    public string? AuditEntityType => nameof(User);
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        // Admin accounts can never be self-registered.
        RuleFor(x => x.Role)
            .Must(r => r is UserRole.Tenant or UserRole.Owner or UserRole.Agency)
            .WithMessage("Role must be Tenant, Owner or Agency.");
        RuleFor(x => x.Password).StrongPassword();
    }
}

public class RegisterCommandHandler(IAppDbContext db, IPasswordHasher hasher, IClock clock)
    : IRequestHandler<RegisterCommand, Guid>
{
    public async Task<Guid> Handle(RegisterCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = request.Role,
            CreatedAt = clock.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user.Id;
    }
}
