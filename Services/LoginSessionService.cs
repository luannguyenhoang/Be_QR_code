using backend.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace backend.Services;

public class LoginSessionService
{
    private readonly ConcurrentDictionary<string, LoginSession> _sessions = new();
    private readonly TimeSpan _sessionExpiry = TimeSpan.FromMinutes(5);

    public LoginSession CreateSession(string baseUrl)
    {
        var sessionId = GenerateSessionId();
        var qrCodeData = $"{baseUrl}/mobile/confirm?sessionId={sessionId}";
        
        var session = new LoginSession
        {
            SessionId = sessionId,
            QrCodeData = qrCodeData,
            Status = LoginStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _sessions[sessionId] = session;
        
        CleanupExpiredSessions();
        
        return session;
    }

    public LoginSession? GetSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            if (DateTime.UtcNow - session.CreatedAt > _sessionExpiry)
            {
                session.Status = LoginStatus.Expired;
                _sessions.TryRemove(sessionId, out _);
                return null;
            }
            return session;
        }
        return null;
    }

    public bool ConfirmLogin(string sessionId, string? userInfo = null)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            if (session.Status == LoginStatus.Pending && 
                DateTime.UtcNow - session.CreatedAt <= _sessionExpiry)
            {
                session.Status = LoginStatus.Confirmed;
                session.ConfirmedAt = DateTime.UtcNow;
                session.UserInfo = userInfo ?? "User";
                return true;
            }
        }
        return false;
    }

    private string GenerateSessionId()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "")
            .Substring(0, 32);
    }

    private void CleanupExpiredSessions()
    {
        var expiredSessions = _sessions
            .Where(kvp => DateTime.UtcNow - kvp.Value.CreatedAt > _sessionExpiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in expiredSessions)
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }
}

