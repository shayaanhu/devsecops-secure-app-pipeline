using System;
using System.ComponentModel.DataAnnotations;
namespace CarpoolApp.Server.Models
{
    public class RideRequest
    {
        public int RideRequestId { get; set; }

        [Required(ErrorMessage = "Pickup location is required.")]
        [StringLength(100, ErrorMessage = "Pickup location cannot exceed 100 characters.")]
        public string PickupLocation { get; set; }

        [Required(ErrorMessage = "Dropoff location is required.")]
        [StringLength(100, ErrorMessage = "Dropoff location cannot exceed 100 characters.")]
        public string DropoffLocation { get; set; }

        public RideRequestStatus Status { get; set; } = RideRequestStatus.Pending;

        [Required(ErrorMessage = "Passenger ID is required.")]
        public int PassengerId { get; set; }

        public Passenger Passenger { get; set; }

        [Required(ErrorMessage = "Ride ID is required.")]
        public int RideId { get; set; }

        public Ride Ride { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }

    public enum RideRequestStatus
    {
        Pending,
        Accepted,
        Denied
    }

}