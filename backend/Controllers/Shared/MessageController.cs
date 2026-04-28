using CarpoolApp.Server.Data;
using CarpoolApp.Server.DTO;
using CarpoolApp.Server.Hubs;
using CarpoolApp.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarpoolApp.Server.Controllers.Shared
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly CarpoolDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public MessageController(CarpoolDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet("ride/{rideId}")]
        public async Task<IActionResult> GetMessagesByRideId(int rideId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var conversation = await _context.Conversations
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.RideId == rideId);

            if (conversation == null)
                return NotFound("Conversation for this ride not found.");

            bool isMember = conversation.Members.Any(cm => cm.UserId == userId);
            if (!isMember)
                return Forbid("You are not a member of this conversation.");

            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversation.ConversationId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.MessageId,
                    m.Content,
                    m.SentAt,
                    senderId = m.SenderId,
                    senderName = m.Sender.FullName
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost("ride/{rideId}/send")]
        public async Task<IActionResult> SendMessageToRide(int rideId, [FromBody] SendMessageDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Get the user's full name for the message
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            var conversation = await _context.Conversations
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.RideId == rideId);

            if (conversation == null)
                return NotFound("Conversation not found for this ride.");

            bool isMember = conversation.Members.Any(m => m.UserId == userId);
            if (!isMember)
                return Forbid("You are not a member of this conversation.");

            var message = new Message
            {
                Content = dto.Content,
                SenderId = userId,
                SentAt = DateTime.UtcNow,
                ConversationId = conversation.ConversationId
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Send the message via SignalR to all clients in the ride group
            await _hubContext.Clients.Group($"ride_{rideId}").SendAsync(
                "ReceiveMessage",
                message.MessageId,
                message.Content,
                user.FullName,
                message.SentAt
            );

            return Ok(new
            {
                messageId = message.MessageId,
                content = message.Content,
                sentAt = message.SentAt,
                senderName = user.FullName
            });
        }
    }
}