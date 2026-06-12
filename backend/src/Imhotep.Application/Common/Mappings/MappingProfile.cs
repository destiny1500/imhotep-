using AutoMapper;
using Imhotep.Application.Common.Models;
using Imhotep.Domain.Entities;

namespace Imhotep.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<Property, PropertyDto>();
        // Name concatenations stay SQL-translatable (FullName is an unmapped computed property).
        CreateMap<Lease, LeaseDto>()
            .ForCtorParam(nameof(LeaseDto.TenantName),
                o => o.MapFrom(l => l.Tenant.FirstName + " " + l.Tenant.LastName));
        CreateMap<Payment, PaymentDto>()
            .ForCtorParam(nameof(PaymentDto.HasReceipt), o => o.MapFrom(p => p.Receipt != null));
        CreateMap<RentReceipt, ReceiptDto>();
        CreateMap<Document, DocumentDto>();
        CreateMap<Notification, NotificationDto>();
        CreateMap<Message, MessageDto>()
            .ForCtorParam(nameof(MessageDto.SenderName),
                o => o.MapFrom(m => m.Sender.FirstName + " " + m.Sender.LastName));
        CreateMap<ConversationParticipant, ParticipantDto>()
            .ForCtorParam(nameof(ParticipantDto.Id), o => o.MapFrom(p => p.UserId))
            .ForCtorParam(nameof(ParticipantDto.Name),
                o => o.MapFrom(p => p.User.FirstName + " " + p.User.LastName))
            .ForCtorParam(nameof(ParticipantDto.Role), o => o.MapFrom(p => p.User.Role));
        CreateMap<Conversation, ConversationDto>();
    }
}
