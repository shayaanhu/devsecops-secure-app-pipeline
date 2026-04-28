using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CarpoolApp.Server.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinRideGroup(string rideId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ride_{rideId}");
        }

        public async Task LeaveRideGroup(string rideId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ride_{rideId}");
        }

        public async Task SendMessage(int rideId, string message, string senderName, int messageId, DateTime sentAt)
        {
            await Clients.Group($"ride_{rideId}").SendAsync("ReceiveMessage", messageId, message, senderName, sentAt);
        }
    }
}