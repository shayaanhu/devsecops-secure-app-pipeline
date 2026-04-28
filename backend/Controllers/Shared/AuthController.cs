using Microsoft.AspNetCore.Mvc;
using CarpoolApp.Server.Models;
using CarpoolApp.Server.DTO;
using Microsoft.EntityFrameworkCore;
using CarpoolApp.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Concurrent;
using CarpoolApp.Server.Services;

namespace CarpoolApp.Server.Controllers.Shared
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CarpoolDbContext _context;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, string> OtpStore = new();
        private readonly EmailService _emailService;

        public AuthController(CarpoolDbContext context, IConfiguration configuration, EmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] OtpRequestDto dto)
        {
            if (string.IsNullOrEmpty(dto?.UniversityEmail))
                return BadRequest(new { success = false, message = "Email is required." });

            if (await _context.Users.AnyAsync(u => u.UniversityEmail == dto.UniversityEmail))
                return BadRequest(new { success = false, message = "Email already exists." });

            string otp = new Random().Next(100000, 999999).ToString();
            OtpStore[dto.UniversityEmail] = otp;

            bool sent = await _emailService.SendOtpEmailAsync(dto.UniversityEmail, otp);
            if (!sent)
                return StatusCode(500, new { success = false, message = "Failed to send OTP. Please try again later." });

            return Ok(new { success = true, message = "OTP sent successfully." });
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] OtpVerificationDto dto)
        {
            if (OtpStore.TryGetValue(dto.UniversityEmail, out var validOtp) && dto.Otp == validOtp)
            {
                OtpStore[dto.UniversityEmail] = "verified";
                return Ok(new { success = true, message = "OTP verified successfully." });
            }

            return BadRequest(new { success = false, message = "Invalid OTP." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.UniversityEmail == dto.UniversityEmail))
                return BadRequest(new { success = false, message = "Email already exists." });

            if (!OtpStore.TryGetValue(dto.UniversityEmail, out var otpStatus) || otpStatus != "verified")
                return BadRequest(new { success = false, message = "OTP not verified." });

            var hasher = new PasswordHasher<User>();

            var user = new User
            {
                FullName = dto.FullName,
                UniversityEmail = dto.UniversityEmail,
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = hasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            OtpStore.TryRemove(dto.UniversityEmail, out _);

            return Ok(new { success = true, message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Driver)
                .Include(u => u.Passenger)
                .FirstOrDefaultAsync(u => u.UniversityEmail == dto.UniversityEmail);

            if (user == null)
                return Unauthorized(new { success = false, message = "Invalid email or password." });

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (result != PasswordVerificationResult.Success)
                return Unauthorized(new { success = false, message = "Invalid email or password." });

            if (dto.Role.ToLower() == "driver" && user.Driver == null)
            {
                _context.Drivers.Add(new Models.Driver { UserId = user.UserId });
                await _context.SaveChangesAsync();
            }
            else if (dto.Role.ToLower() == "passenger" && user.Passenger == null)
            {
                _context.Passengers.Add(new Models.Passenger { UserId = user.UserId });
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user, dto.Role.ToLower());

            return Ok(new
            {
                success = true,
                message = "Login successful.",
                token,
                userId = user.UserId,
                role = dto.Role
            });
        }

        private string GenerateJwtToken(User user, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.UniversityEmail),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class OtpRequestDto
    {
        public string UniversityEmail { get; set; }
    }

    public class OtpVerificationDto
    {
        public string UniversityEmail { get; set; }
        public string Otp { get; set; }
    }
}
