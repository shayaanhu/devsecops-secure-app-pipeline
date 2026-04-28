using System.ComponentModel.DataAnnotations;

namespace CarpoolApp.Server.DTO
{
    //Ride RequestDto
    public class RideRequestDto
    {
        [Required(ErrorMessage = "Pickup location is required.")]
        public string PickupLocation { get; set; }

        [Required(ErrorMessage = "Dropoff location is required.")]
        public string DropoffLocation { get; set; }

        [Required(ErrorMessage = "Ride ID is required.")]
        public int RideId { get; set; }
    }

    // RideLocationsDto.cs
    public class RideLocationsDto
    {
        public int RideId { get; set; }
        public List<string> Locations { get; set; } = new List<string>();
    }

}