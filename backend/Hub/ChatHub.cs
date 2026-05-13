using CarpoolApp.Server.Data;
using CarpoolApp.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarpoolApp.Server.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly CarpoolDbContext _context;

        public ChatHub(CarpoolDbContext context)
        {
            _context = context;
        }

        public async Task JoinRideGroup(string rideId)
        {
            if (!int.TryParse(rideId, out var parsedRideId) || !await IsRideConversationMember(parsedRideId))
                throw new HubException("You are not authorized to join this ride chat.");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"ride_{parsedRideId}");
        }

        public async Task LeaveRideGroup(string rideId)
        {
            if (int.TryParse(rideId, out var parsedRideId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ride_{parsedRideId}");
        }

        public async Task SendMessage(int rideId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new HubException("Message content is required.");

            if (message.Length > 1000)
                throw new HubException("Message cannot exceed 1000 characters.");

            if (!await IsRideConversationMember(rideId))
                throw new HubException("You are not authorized to send messages to this ride chat.");

            var userId = GetAuthenticatedUserId();
            var user = await _context.Users.FindAsync(userId);
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.RideId == rideId);

            if (user == null || conversation == null)
                throw new HubException("Ride conversation could not be found.");

            var savedMessage = new Message
            {
                Content = message.Trim(),
                SenderId = userId,
                SentAt = DateTime.UtcNow,
                ConversationId = conversation.ConversationId
            };

            _context.Messages.Add(savedMessage);
            await _context.SaveChangesAsync();

            await Clients.Group($"ride_{rideId}").SendAsync(
                "ReceiveMessage",
                savedMessage.MessageId,
                savedMessage.Content,
                user.FullName,
                savedMessage.SentAt);
        }

        private int GetAuthenticatedUserId()
        {
            var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : 0;
        }

        private async Task<bool> IsRideConversationMember(int rideId)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == 0)
                return false;

            return await _context.Conversations
                .Where(c => c.RideId == rideId)
                .SelectMany(c => c.Members)
                .AnyAsync(m => m.UserId == userId);
        }
    }
}
