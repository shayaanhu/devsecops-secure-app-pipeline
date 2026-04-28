using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace CarpoolApp.Server.Models
{
    public class Vehicle
    {
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Make is required.")]
        [StringLength(10, ErrorMessage = "Make cannot exceed 10 characters.")]
        [RegularExpression("^[A-Za-z]+$", ErrorMessage = "Make can only contain letters.")]
        public string Make { get; set; }

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(10, ErrorMessage = "Model cannot exceed 10 characters.")]
        public string Model { get; set; }

        [Required(ErrorMessage = "Number Plate is required.")]
        [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Number Plate can only contain letters, numbers, and hyphens.")]
        [StringLength(7, ErrorMessage = "Number Plate cannot exceed 7 characters.")]
        public string NumberPlate { get; set; }

        [Required(ErrorMessage = "Driver ID is required.")]
        public int DriverId { get; set; }

        public Driver Driver { get; set; }

        public ICollection<Ride> Rides { get; set; } = new List<Ride>();
    }

}