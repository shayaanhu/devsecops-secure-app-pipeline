using Microsoft.AspNetCore.Mvc;
using CarpoolApp.Server.Models;
using CarpoolApp.Server.DTO;
using Microsoft.EntityFrameworkCore;
using CarpoolApp.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;
using CarpoolApp.Server.Services;
using Microsoft.AspNetCore.Authorization;

namespace CarpoolApp.Server.Controllers.Shared
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CarpoolDbContext _context;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, OtpRecord> OtpStore = new(StringComparer.OrdinalIgnoreCase);
        private readonly EmailService _emailService;
        private readonly TokenBlacklistService _blacklist;
        private const int OtpExpiryMinutes = 10;
        private const int MaxOtpAttempts = 5;
        private static readonly TimeSpan OtpResendDelay = TimeSpan.FromSeconds(60);

        public AuthController(CarpoolDbContext context, IConfiguration configuration, EmailService emailService, TokenBlacklistService blacklist)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _blacklist = blacklist;
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] OtpRequestDto dto)
        {
            if (string.IsNullOrEmpty(dto?.UniversityEmail))
                return BadRequest(new { success = false, message = "Email is required." });

            var email = dto.UniversityEmail.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.UniversityEmail == email))
                return BadRequest(new { success = false, message = "Email already exists." });

            if (OtpStore.TryGetValue(email, out var existingOtp)
                && DateTime.UtcNow - existingOtp.LastSentAt < OtpResendDelay)
            {
                return StatusCode(429, new { success = false, message = "Please wait before requesting another OTP." });
            }

            string otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            OtpStore[email] = new OtpRecord
            {
                OtpHash = HashOtp(email, otp),
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                LastSentAt = DateTime.UtcNow
            };

            // Send OTP via email (logging is handled in EmailService)
            await _emailService.SendOtpEmailAsync(email, otp);
            
            return Ok(new { success = true, message = "OTP sent successfully. Check your email or backend logs for the code." });
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] OtpVerificationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.UniversityEmail) || string.IsNullOrWhiteSpace(dto.Otp))
                return BadRequest(new { success = false, message = "Email and OTP are required." });

            var email = dto.UniversityEmail.Trim().ToLowerInvariant();
            if (!OtpStore.TryGetValue(email, out var otpRecord))
                return BadRequest(new { success = false, message = "Invalid OTP." });

            if (DateTime.UtcNow > otpRecord.ExpiresAt)
            {
                OtpStore.TryRemove(email, out _);
                return BadRequest(new { success = false, message = "OTP expired." });
            }

            if (otpRecord.Attempts >= MaxOtpAttempts)
            {
                OtpStore.TryRemove(email, out _);
                return StatusCode(429, new { success = false, message = "Too many invalid OTP attempts." });
            }

            if (FixedTimeEquals(otpRecord.OtpHash, HashOtp(email, dto.Otp)))
            {
                otpRecord.Verified = true;
                return Ok(new { success = true, message = "OTP verified successfully." });
            }

            otpRecord.Attempts++;
            return BadRequest(new { success = false, message = "Invalid OTP." });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            dto.UniversityEmail = dto.UniversityEmail.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.UniversityEmail == dto.UniversityEmail))
                return BadRequest(new { success = false, message = "Email already exists." });

            if (!OtpStore.TryGetValue(dto.UniversityEmail, out var otpStatus)
                || !otpStatus.Verified
                || DateTime.UtcNow > otpStatus.ExpiresAt)
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

            if (dto.Role.ToLower() == "admin")
            {
                var adminEmail = _configuration["AdminSettings:Email"];
                if (string.IsNullOrWhiteSpace(adminEmail)
                    || !string.Equals(user.UniversityEmail, adminEmail, StringComparison.OrdinalIgnoreCase))
                    return Unauthorized(new { success = false, message = "Not authorized as admin." });
                var adminToken = GenerateJwtToken(user, "admin");
                return Ok(new { success = true, message = "Admin login successful.", token = adminToken, userId = user.UserId, role = "admin" });
            }

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

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(jti))
                return BadRequest(new { success = false, message = "Token has no JTI claim." });

            var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
            DateTime expiry = DateTime.UtcNow.AddHours(6);
            if (long.TryParse(expClaim, out var expUnix))
                expiry = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

            _blacklist.Revoke(jti, expiry);
            return Ok(new { success = true, message = "Logged out successfully." });
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            try
            {
                var userCount = await _context.Users.CountAsync();
                var adminExists = await _context.Users.AnyAsync(u => u.UniversityEmail == _configuration["AdminSettings:Email"]);
                return Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    userCount = userCount,
                    adminExists = adminExists,
                    databaseConnected = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "unhealthy", error = ex.Message });
            }
        }

        private string GenerateJwtToken(User user, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.UniversityEmail),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
                throw new InvalidOperationException("Jwt:Key is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
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

        private static string HashOtp(string email, string otp)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{email}:{otp}"));
            return Convert.ToHexString(bytes);
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(left),
                Encoding.UTF8.GetBytes(right));
        }

        private sealed class OtpRecord
        {
            public string OtpHash { get; set; }
            public DateTime ExpiresAt { get; set; }
            public DateTime LastSentAt { get; set; }
            public int Attempts { get; set; }
            public bool Verified { get; set; }
        }
    }
}
