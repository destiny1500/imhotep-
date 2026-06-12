using AutoMapper;
using Imhotep.Application.Common.Exceptions;
using Imhotep.Application.Common.Interfaces;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imhotep.Application.Features.Auth;

public record GetMeQuery : IRequest<UserDto>;

public class GetMeQueryHandler(IAppDbContext db, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<GetMeQuery, UserDto>
{
    public async Task<UserDto> Handle(GetMeQuery request, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new NotFoundException(nameof(User), currentUser.UserId ?? Guid.Empty);
        return mapper.Map<UserDto>(user);
    }
}
