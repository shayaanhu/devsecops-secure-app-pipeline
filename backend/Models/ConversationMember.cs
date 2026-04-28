using System.ComponentModel.DataAnnotations;

namespace CarpoolApp.Server.Models
{
    public class ConversationMember
    {
        [Required(ErrorMessage = "Conversation ID is required.")]
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }

        [Required(ErrorMessage = "User ID is required.")]
        public int UserId { get; set; }
        public User User { get; set; }
    }
}