using System.Collections.Concurrent;

namespace CarpoolApp.Server.Services
{
    public class TokenBlacklistService
    {
        private readonly ConcurrentDictionary<string, DateTime> _blacklist = new();

        public void Revoke(string jti, DateTime expiry)
        {
            _blacklist[jti] = expiry;
        }

        public bool IsRevoked(string jti)
        {
            if (_blacklist.TryGetValue(jti, out var expiry))
            {
                if (DateTime.UtcNow > expiry)
                {
                    _blacklist.TryRemove(jti, out _);
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
