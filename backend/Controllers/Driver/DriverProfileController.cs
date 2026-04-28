using CarpoolApp.Server.Data;
using CarpoolApp.Server.DTO;
using CarpoolApp.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarpoolApp.Server.Controllers.Driver
{
    [Authorize(Roles = "driver")]
    [ApiController]
    [Route("api/driver/profile")]
    public class DriverProfileController : ControllerBase
    {
        private readonly CarpoolDbContext _context;

        public DriverProfileController(CarpoolDbContext context)
        {
            _context = context;
        }

        [HttpPost("vehicle")]
        public async Task<IActionResult> AddVehicle([FromBody] CreateVehicleDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return BadRequest(new { success = false, message = "Validation failed", errors });
            }

            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

            if (driver == null)
                return NotFound(new { success = false, message = "Driver not found." });

            var vehicle = new Vehicle
            {
                Make = dto.Make,
                Model = dto.Model,
                NumberPlate = dto.NumberPlate,
                DriverId = driver.DriverId
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Vehicle added successfully." });
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var vehicles = await _context.Vehicles
                .Where(v => v.Driver.UserId == userId)
                .Select(v => new
                {
                    v.VehicleId,
                    v.Make,
                    v.Model,
                    v.NumberPlate
                })
                .ToListAsync();

            return Ok(vehicles);
        }
    }
}