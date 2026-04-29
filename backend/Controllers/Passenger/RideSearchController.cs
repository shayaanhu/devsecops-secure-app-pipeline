using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using CarpoolApp.Server.Models;
using CarpoolApp.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using CarpoolApp.Server.DTO;

namespace CarpoolApp.Server.Controllers.Passenger
{
    [Authorize(Roles = "passenger")]
    [Route("api/[controller]")]
    [ApiController]
    public class RideSearchController : ControllerBase
    {
        private readonly CarpoolDbContext _context;

        public RideSearchController(CarpoolDbContext context)
        {
            _context = context;
        }

        [HttpGet("search")]
        public IActionResult SearchRides([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search term is required.");

            query = query.Trim();

            var sql = "SELECT * FROM Rides WHERE Status = 0 AND DepartureTime >= datetime('now') " +
                      "AND (Origin LIKE '%" + query + "%' OR Destination LIKE '%" + query + "%')";

            var rides = _context.Rides.FromSqlRaw(sql)
                .Include(r => r.Driver)
                    .ThenInclude(d => d.User)
                .Include(r => r.Vehicle)
                .Include(r => r.RideRequests)
                .AsEnumerable()
                .Where(r => r.AvailableSeats > r.RideRequests.Count(rr => rr.Status == RideRequestStatus.Accepted))
                .Select(r => new RideSearchResultDto
                {
                    RideId = r.RideId,
                    Origin = r.Origin,
                    Destination = r.Destination,
                    DepartureTime = r.DepartureTime.ToString("o"),
                    AvailableSeats = r.AvailableSeats - r.RideRequests.Count(rr => rr.Status == RideRequestStatus.Accepted),
                    PricePerSeat = r.PricePerSeat,
                    DriverName = r.Driver?.User?.FullName ?? "Unknown Driver",
                    VehicleModel = r.Vehicle?.Model ?? "Unknown Vehicle",
                    RouteStops = string.IsNullOrEmpty(r.RouteStops)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(r.RouteStops)
                })
                .ToList();

            if (!rides.Any())
                return NotFound("No rides found for the given search term.");

            return Ok(rides);
        }

    }
}