using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace CarpoolApp.Server.Models
{
    public class Passenger
    {
        public int PassengerId { get; set; }

        [Required(ErrorMessage = "User ID is required.")]
        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<RideRequest> RideRequests { get; set; } = new List<RideRequest>();
    }

}