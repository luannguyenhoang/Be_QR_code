namespace backend.Models;

public class LoginSession
{
    public string SessionId { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty;
    public LoginStatus Status { get; set; } = LoginStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public string? UserInfo { get; set; }
}

public enum LoginStatus
{
    Pending,
    Confirmed,
    Expired
}

