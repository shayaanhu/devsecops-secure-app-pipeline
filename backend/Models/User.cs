using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CarpoolApp.Server.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "University Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        //[RegularExpression(@"^[a-zA-Z0-9._%+-]+@([a-zA-Z0-9.-]+\.)?(edu|ac)\.([a-zA-Z]{2,})$",
        //    ErrorMessage = "Only university email addresses are allowed.")]
        public string UniversityEmail { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string PasswordHash { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Driver? Driver { get; set; }
        public Passenger? Passenger { get; set; }

        public ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();

        public ICollection<Message> Messages { get; set; } = new List<Message>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}