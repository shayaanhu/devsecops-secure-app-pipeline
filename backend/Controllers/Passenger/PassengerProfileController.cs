using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CarpoolApp.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CarpoolApp.Server.Models;

namespace CarpoolApp.Server.Controllers.Passenger
{
    [Authorize(Roles = "passenger")]
    [Route("api/[controller]")]
    [ApiController]
    public class PassengerProfileController : ControllerBase
    {
        private readonly CarpoolDbContext _context;
        public PassengerProfileController(CarpoolDbContext context)
        {
            _context = context;
        }

        [HttpGet("accepted-rides")]
        public async Task<IActionResult> GetUserAcceptedRides()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var acceptedRides = await _context.RideRequests
                    .Where(rq => rq.Passenger.UserId == userId && rq.Status == RideRequestStatus.Accepted)
                    .Include(rq => rq.Ride)
                        .ThenInclude(r => r.Vehicle)
                    .Include(rq => rq.Ride)
                        .ThenInclude(r => r.Driver)
                            .ThenInclude(d => d.User)
                .Select(rq => new
                {
                    rideId = rq.Ride.RideId,
                    origin = rq.Ride.Origin,
                    destination = rq.Ride.Destination,
                    departureTime = rq.Ride.DepartureTime,
                    vehicle = rq.Ride.Vehicle != null ? $"{rq.Ride.Vehicle.Make} {rq.Ride.Vehicle.Model} - {rq.Ride.Vehicle.NumberPlate}" : "No vehicle",
                    driverName = rq.Ride.Driver.User.FullName,
                    pickupLocation = rq.PickupLocation == "To be decided" && rq.Ride.Origin.ToLower().Contains("habib university")
    ? rq.Ride.Origin
    : rq.PickupLocation,

                    dropoffLocation = rq.DropoffLocation == "To be decided" && rq.Ride.Destination.ToLower().Contains("habib university")
        ? rq.Ride.Destination
        : rq.DropoffLocation
                })

                    .ToListAsync();

                return Ok(acceptedRides);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}