using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CarpoolApp.Server.Models
{
    public class Ride
    {
        public int RideId { get; set; }

        [Required(ErrorMessage = "Origin is required.")]
        [StringLength(100, ErrorMessage = "Origin cannot exceed 100 characters.")]
        public string Origin { get; set; }

        [Required(ErrorMessage = "Destination is required.")]
        [StringLength(100, ErrorMessage = "Destination cannot exceed 100 characters.")]
        public string Destination { get; set; }

        public RideStatus Status { get; set; } = RideStatus.Scheduled;

        // Changed to string with custom formatting/parsing
        public string RouteStops { get; set; }

        [Required(ErrorMessage = "Departure time is required.")]
        public DateTime DepartureTime { get; set; }

        [Required(ErrorMessage = "Available seats are required.")]
        [Range(1, 10, ErrorMessage = "Available seats must be between 1 and 10.")]
        public int AvailableSeats { get; set; }

        [Required(ErrorMessage = "Price per seat is required.")]
        [Range(150, 500, ErrorMessage = "Price per seat must be between 150 and 500.")]
        public int PricePerSeat { get; set; }

        [Required(ErrorMessage = "Driver ID is required.")]
        public int DriverId { get; set; }

        public Driver Driver { get; set; }

        [Required(ErrorMessage = "Vehicle ID is required.")]
        public int VehicleId { get; set; }

        public Vehicle Vehicle { get; set; }

        public ICollection<RideRequest> RideRequests { get; set; } = new List<RideRequest>();
    }

    public enum RideStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Canceled
    }


}