﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CarpoolApp.Server.Data;
using CarpoolApp.Server.Models;
using CarpoolApp.Server.DTO;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;

namespace CarpoolApp.Server.Controllers.Passenger
{
    [Authorize(Roles = "passenger")]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly CarpoolDbContext _context;

        public BookingController(CarpoolDbContext context)
        {
            _context = context;
        }

        [HttpPost("request-ride")]
        public async Task<IActionResult> RequestRide([FromBody] RideRequestDto requestDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized("Invalid passenger credentials.");

            var passenger = await _context.Passengers.FirstOrDefaultAsync(p => p.UserId == int.Parse(userId));
            if (passenger == null)
                return NotFound("Passenger record not found.");

            var existingRequest = await _context.RideRequests
                .FirstOrDefaultAsync(r => r.PassengerId == passenger.PassengerId && r.RideId == requestDto.RideId);

            if (existingRequest != null && existingRequest.Status != RideRequestStatus.Denied)
                return BadRequest("You have already requested this ride.");

            if (existingRequest != null && existingRequest.Status == RideRequestStatus.Denied)
            {
                _context.RideRequests.Remove(existingRequest);
                await _context.SaveChangesAsync();
            }

            var rideRequest = new RideRequest
            {
                PickupLocation = requestDto.PickupLocation,
                DropoffLocation = requestDto.DropoffLocation,
                PassengerId = passenger.PassengerId,
                RideId = requestDto.RideId,
                Status = RideRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _context.RideRequests.Add(rideRequest);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Ride request sent successfully!" });
        }

        [HttpGet("ride-locations/{rideId}")]
        public async Task<IActionResult> GetRideLocations(int rideId)
        {
            var ride = await _context.Rides.FirstOrDefaultAsync(r => r.RideId == rideId);
            if (ride == null)
                return NotFound("Ride not found.");

            List<string> routeStops = new List<string>();
            if (!string.IsNullOrWhiteSpace(ride.RouteStops))
            {
                try
                {
                    routeStops = JsonSerializer.Deserialize<List<string>>(ride.RouteStops);
                }
                catch(Exception ex) 
                {
                    Console.WriteLine(ex);
                }
            }

            var locations = new List<string> { ride.Origin };
            locations.AddRange(routeStops);
            locations.Add(ride.Destination);

            return Ok(new RideLocationsDto
            {
                RideId = ride.RideId,
                Locations = locations
            });
        }
    }
}