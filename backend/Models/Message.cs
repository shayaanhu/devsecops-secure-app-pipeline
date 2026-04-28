using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualBasic;

namespace CarpoolApp.Server.Models
{
    public class Message
    {
        public int MessageId { get; set; }

        [Required(ErrorMessage = "Message content is required.")]
        [StringLength(1000, ErrorMessage = "Message cannot exceed 1000 characters.")]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Sender ID is required.")]
        public int SenderId { get; set; }
        public User Sender { get; set; }

        [Required(ErrorMessage = "Conversation ID is required.")]
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; }
    }

}