using CarpoolApp.Server.Data;
using CarpoolApp.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarpoolApp.Server.Controllers.Admin
{
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly CarpoolDbContext _context;

        public AdminController(CarpoolDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalRides = await _context.Rides.CountAsync();
            var activeRides = await _context.Rides
                .CountAsync(r => r.Status == RideStatus.Scheduled || r.Status == RideStatus.InProgress);

            return Ok(new { totalUsers, totalRides, activeRides });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.UniversityEmail,
                    u.PhoneNumber,
                    u.CreatedAt,
                    isDriver = u.Driver != null,
                    isPassenger = u.Passenger != null
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found." });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "User deleted." });
        }

        [HttpGet("rides")]
        public async Task<IActionResult> GetRides()
        {
            var rides = await _context.Rides
                .Include(r => r.Driver)
                    .ThenInclude(d => d.User)
                .Include(r => r.Vehicle)
                .Select(r => new
                {
                    r.RideId,
                    r.Origin,
                    r.Destination,
                    r.DepartureTime,
                    r.AvailableSeats,
                    r.PricePerSeat,
                    status = r.Status.ToString(),
                    driverName = r.Driver != null && r.Driver.User != null ? r.Driver.User.FullName : "Unknown",
                    vehicle = r.Vehicle != null ? $"{r.Vehicle.Make} {r.Vehicle.Model} - {r.Vehicle.NumberPlate}" : "No vehicle"
                })
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync();

            return Ok(rides);
        }

        [HttpPatch("rides/{id}/cancel")]
        public async Task<IActionResult> CancelRide(int id)
        {
            var ride = await _context.Rides.FindAsync(id);
            if (ride == null)
                return NotFound(new { message = "Ride not found." });

            ride.Status = RideStatus.Canceled;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Ride cancelled." });
        }
    }
}
