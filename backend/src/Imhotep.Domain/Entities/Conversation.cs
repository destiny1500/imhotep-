using Imhotep.Domain.Common;

namespace Imhotep.Domain.Entities;

public class Conversation : BaseEntity
{
    public string Subject { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public Property? Property { get; set; }
    public DateTime LastMessageAtUtc { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public bool HasParticipant(Guid userId) => Participants.Any(p => p.UserId == userId);
}

public class ConversationParticipant
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
}
