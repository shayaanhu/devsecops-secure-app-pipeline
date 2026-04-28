using System.ComponentModel.DataAnnotations;

namespace CarpoolApp.Server.Models
{
    public class Conversation
    {
        public int ConversationId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? RideId { get; set; }
        public Ride? Ride { get; set; }

        // [Required(ErrorMessage = "A conversation must have at least one member.")]
        public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }

}