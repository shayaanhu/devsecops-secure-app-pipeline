using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CarpoolApp.Server.Models
{
    public class Driver
    {
        public int DriverId { get; set; }

        [Required(ErrorMessage = "User ID is required.")]
        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public ICollection<Ride> Rides { get; set; } = new List<Ride>();
    }

}