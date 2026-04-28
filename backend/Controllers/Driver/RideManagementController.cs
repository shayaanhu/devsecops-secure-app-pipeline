using CarpoolApp.Server.Data;
using CarpoolApp.Server.DTO;
using CarpoolApp.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Security.Claims;
using System.Text.Json;

namespace CarpoolApp.Server.Controllers.Driver
{
    [Authorize(Roles = "driver")]
    [Route("api/[controller]")]
    [ApiController]
    public class RideManagementController : ControllerBase
    {
        private readonly CarpoolDbContext _context;

        public RideManagementController(CarpoolDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRide([FromBody] CreateRideDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var driver = await _context.Drivers
                .Include(d => d.Vehicles)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
                return NotFound("Driver not found.");

            var vehicle = driver.Vehicles?.FirstOrDefault(v => v.VehicleId == dto.VehicleId);
            if (vehicle == null)
                return BadRequest("Invalid vehicle selection or vehicle not owned by this driver.");

            var ride = new Ride
            {
                Origin = dto.Origin,
                Destination = dto.Destination,
                RouteStops = JsonSerializer.Serialize(dto.RouteStops),
                DepartureTime = dto.DepartureTime,
                VehicleId = dto.VehicleId,
                DriverId = driver.DriverId,
                AvailableSeats = dto.AvailableSeats,
                PricePerSeat = dto.PricePerSeat,
            };

            _context.Rides.Add(ride);
            await _context.SaveChangesAsync();

            var conversation = new Conversation
            {
                RideId = ride.RideId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync(); // Save to generate ConversationId

            var member = new ConversationMember
            {
                ConversationId = conversation.ConversationId,
                UserId = userId
            };

            _context.ConversationMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Ride created successfully.", rideId = ride.RideId });
        }


        [HttpGet("accepted-passengers/{rideId}")]
        public async Task<IActionResult> GetAcceptedPassengers(int rideId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            // Check ride ownership and get accepted passengers in a single query
            var rideWithPassengers = await _context.Rides
                .Where(r => r.RideId == rideId && r.Driver.UserId == userId)
                .Select(r => new
                {
                    ride = r,
                    acceptedPassengers = r.RideRequests
                        .Where(req => req.Status == RideRequestStatus.Accepted)
                        .Select(req => new
                        {
                            requestId = req.RideRequestId,
                            pickupLocation = req.PickupLocation,
                            dropoffLocation = req.DropoffLocation,
                            passengerName = req.Passenger.User.FullName,
                            passengerPhone = req.Passenger.User.PhoneNumber
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (rideWithPassengers == null)
                return NotFound("Ride not found or not associated with this driver.");

            return Ok(rideWithPassengers.acceptedPassengers);
        }
    }
}