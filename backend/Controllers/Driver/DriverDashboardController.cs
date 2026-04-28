using CarpoolApp.Server.Data;
using CarpoolApp.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CarpoolApp.Server.Controllers.Driver
{
    [Authorize(Roles = "driver")]
    [ApiController]
    [Route("api/driver/dashboard")]
    public class DriverDashboardController : ControllerBase
    {
        private readonly CarpoolDbContext _context;

        public DriverDashboardController(CarpoolDbContext context)
        {
            _context = context;
        }

        [HttpGet("rides-with-requests")]
        public async Task<IActionResult> GetRidesWithRequests()
        {
            try
            {
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
                if (driver == null)
                    return Unauthorized(new { message = "Driver profile not found." });

                var rides = await _context.Rides
                    .Where(r => r.DriverId == driver.DriverId)
                    .Include(r => r.Vehicle)
                    .Include(r => r.RideRequests)
                        .ThenInclude(req => req.Passenger)
                            .ThenInclude(p => p.User)
                    .AsSplitQuery()
                    .OrderByDescending(r => r.DepartureTime)
                    .ToListAsync();

                var ridesWithRequests = rides.Select(r => new
                {
                    rideId = r.RideId,
                    origin = r.Origin,
                    destination = r.Destination,
                    departureTime = r.DepartureTime,
                    availableSeats = r.AvailableSeats,
                    pricePerSeat = r.PricePerSeat,
                    vehicle = r.Vehicle != null ? $"{r.Vehicle.Make} {r.Vehicle.Model} - {r.Vehicle.NumberPlate}" : "No Vehicle",
                    routeStops = string.IsNullOrEmpty(r.RouteStops)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(r.RouteStops),

                    // ⬇️ Separate pending requests
                    requests = (r.RideRequests ?? new List<RideRequest>())
                        .Where(req => req.Status == RideRequestStatus.Pending)
                        .Select(req => new
                            
                        {
                            requestId = req.RideRequestId,
                            pickupLocation = req.PickupLocation,
                            dropoffLocation = req.DropoffLocation,
                            passengerName = req.Passenger?.User?.FullName ?? "Unknown"
                        })
                        .ToList(),

                    // ⬇️ And accepted passengers separately
                    acceptedPassengers = (r.RideRequests ?? new List<RideRequest>())
                        .Where(req => req.Status == RideRequestStatus.Accepted)
                        .Select(req => new
                        {
                            requestId = req.RideRequestId,
                            pickupLocation = req.PickupLocation,
                            dropoffLocation = req.DropoffLocation,
                            passengerName = req.Passenger?.User?.FullName ?? "Unknown"
                        })
                        .ToList()
                }).ToList();

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    result = ridesWithRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching rides.", error = ex.Message });
            }
        }
    }
}
