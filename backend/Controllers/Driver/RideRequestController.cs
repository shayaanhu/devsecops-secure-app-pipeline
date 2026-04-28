using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CarpoolApp.Server.Data;
using CarpoolApp.Server.Models;
using Microsoft.VisualBasic;

namespace CarpoolApp.Server.Controllers.Driver
{
    [Route("api/riderequest")]
    [ApiController]
    [Authorize(Roles = "driver")]
    public class RideRequestController : ControllerBase
    {
        private readonly CarpoolDbContext _context;

        public RideRequestController(CarpoolDbContext context)
        {
            _context = context;
        }

        [HttpPost("accept/{requestId}")]
        public async Task<IActionResult> AcceptRideRequest(int requestId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { message = "User not authenticated" });

            int userId = int.Parse(userIdClaim);

            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
            if (driver == null)
                return NotFound(new { message = "Driver not found" });

            int driverId = driver.DriverId;

            var rideRequest = await _context.RideRequests
                .Include(r => r.Ride)
                .Include(r => r.Passenger)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(r => r.RideRequestId == requestId && r.Ride.DriverId == driverId);

            if (rideRequest == null)
                return NotFound("Ride request not found or unauthorized.");

            rideRequest.Status = RideRequestStatus.Accepted;
            await _context.SaveChangesAsync();

            // Check if a conversation exists for this ride
            var conversation = await _context.Conversations
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.RideId == rideRequest.RideId);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    RideId = rideRequest.RideId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync(); // Save to get ConversationId
            }

            // Ensure user is not already a member
            bool alreadyMember = conversation.Members
                .Any(cm => cm.UserId == rideRequest.Passenger.UserId);

            if (!alreadyMember)
            {
                var newMember = new ConversationMember
                {
                    ConversationId = conversation.ConversationId,
                    UserId = rideRequest.Passenger.UserId
                };

                _context.ConversationMembers.Add(newMember);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Ride request accepted and conversation member added." });
        }


        [HttpPost("reject/{requestId}")]
        public async Task<IActionResult> RejectRideRequest(int requestId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            int userId = int.Parse(userIdClaim);

            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
            if (driver == null)
            {
                return NotFound(new { message = "Driver not found" });
            }

            int driverId = driver.DriverId;

            var rideRequest = await _context.RideRequests
                .Include(r => r.Ride)
                .FirstOrDefaultAsync(r => r.RideRequestId == requestId && r.Ride.DriverId == driverId);

            if (rideRequest == null)
            {
                return NotFound("Ride request not found or unauthorized.");
            }

            rideRequest.Status = RideRequestStatus.Denied;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ride request rejected." });
        }
    }
}