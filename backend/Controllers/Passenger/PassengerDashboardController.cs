using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using CarpoolApp.Server.Data;
using CarpoolApp.Server.Models;
using CarpoolApp.Server.DTO;

namespace CarpoolApp.Server.Controllers.Passenger
{
    [Authorize(Roles = "passenger")]
    [Route("api/[controller]")]
    [ApiController]
    public class PassengerDashboardController : ControllerBase
    {
        private readonly CarpoolDbContext _context;

        public PassengerDashboardController(CarpoolDbContext context)
        {
            _context = context;
        }

        [HttpGet("available-rides")]
        public IActionResult GetAvailableRides()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized("Invalid passenger credentials.");

            var passenger = _context.Passengers.FirstOrDefault(p => p.UserId == int.Parse(userId));
            if (passenger == null)
                return NotFound("Passenger record not found.");

            var availableRides = _context.Rides
                .Include(r => r.Driver).ThenInclude(d => d.User)
                .Include(r => r.Vehicle)
                .Include(r => r.RideRequests)
                .Where(r =>
                    r.Status == RideStatus.Scheduled &&
                    r.DepartureTime > DateTime.Now &&
                    r.AvailableSeats > r.RideRequests.Count(rr => rr.Status == RideRequestStatus.Accepted) &&
                    !_context.RideRequests.Any(rr => rr.RideId == r.RideId && rr.PassengerId == passenger.PassengerId && rr.Status == RideRequestStatus.Accepted)
                )
                .AsEnumerable()
                .Select(r => new
                {
                    r.RideId,
                    r.Origin,
                    r.Destination,
                    DepartureTime = r.DepartureTime,
                    AvailableSeats = r.AvailableSeats - r.RideRequests.Count(rr => rr.Status == RideRequestStatus.Accepted),
                    r.PricePerSeat,
                    DriverName = r.Driver?.User?.FullName ?? "Unknown Driver",
                    Vehicle = r.Vehicle != null ? $"{r.Vehicle.Make} {r.Vehicle.Model} - {r.Vehicle.NumberPlate}" : "Unknown Vehicle",
                    RouteStops = string.IsNullOrEmpty(r.RouteStops)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(r.RouteStops),
                    RideRequestStatus = _context.RideRequests
                        .Where(rr => rr.RideId == r.RideId && rr.PassengerId == passenger.PassengerId)
                        .Select(rr => rr.Status.ToString())
                        .FirstOrDefault() ?? "Not Requested"
                })
                .ToList();

            return Ok(availableRides);
        }

        [HttpGet("accepted-rides")]
        public IActionResult GetAcceptedRides()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized("Invalid passenger credentials.");

            var passenger = _context.Passengers.FirstOrDefault(p => p.UserId == int.Parse(userId));
            if (passenger == null)
                return NotFound("Passenger record not found.");

            var acceptedRides = _context.Rides
                .Include(r => r.Driver)
                    .ThenInclude(d => d.User)
                .Include(r => r.Vehicle)
                .Where(r => _context.RideRequests.Any(rr => rr.RideId == r.RideId
                                                            && rr.PassengerId == passenger.PassengerId
                                                            && rr.Status == RideRequestStatus.Accepted))
                .AsEnumerable()
                .Select(r => new
                {
                    r.RideId,
                    r.Origin,
                    r.Destination,
                    DepartureTime = r.DepartureTime,
                    r.AvailableSeats,
                    r.PricePerSeat,
                    DriverName = r.Driver?.User?.FullName ?? "Unknown Driver",
                    Vehicle = r.Vehicle != null ? $"{r.Vehicle.Make} {r.Vehicle.Model} - {r.Vehicle.NumberPlate}" : "Unknown Vehicle",
                    RouteStops = string.IsNullOrEmpty(r.RouteStops)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(r.RouteStops),
                })
                .ToList();

            return Ok(acceptedRides);
        }

        [HttpPost("request-ride")]
        public async Task<IActionResult> RequestRide([FromBody] RideRequestDto request)
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var passenger = await _context.Passengers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (passenger == null)
                    return NotFound(new { message = "Passenger not found." });

                var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideId == request.RideId);
                if (ride == null)
                    return NotFound(new { message = "Ride not found." });

                var rideRequest = new RideRequest
                {
                    PickupLocation = request.PickupLocation, // ✅ fixed!
                    DropoffLocation = request.DropoffLocation,
                    PassengerId = passenger.PassengerId,
                    RideId = request.RideId,
                    Status = RideRequestStatus.Pending,
                    RequestedAt = DateTime.UtcNow
                };


                _context.RideRequests.Add(rideRequest);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Ride request sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }
    }
}


