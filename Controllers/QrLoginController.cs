using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrLoginController : ControllerBase
{
    private readonly LoginSessionService _sessionService;
    private readonly QrCodeService _qrCodeService;
    private readonly IConfiguration _configuration;

    public QrLoginController(
        LoginSessionService sessionService,
        QrCodeService qrCodeService,
        IConfiguration configuration)
    {
        _sessionService = sessionService;
        _qrCodeService = qrCodeService;
        _configuration = configuration;
    }

    [HttpPost("generate")]
    public IActionResult GenerateQrCode()
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? Request.Scheme + "://" + Request.Host;
        var session = _sessionService.CreateSession(frontendUrl);
        var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(session.QrCodeData);

        return Ok(new
        {
            sessionId = session.SessionId,
            qrCode = $"data:image/png;base64,{qrCodeBase64}",
            expiresAt = session.CreatedAt.AddMinutes(5)
        });
    }

    [HttpGet("status/{sessionId}")]
    public IActionResult GetStatus(string sessionId)
    {
        var session = _sessionService.GetSession(sessionId);
        
        if (session == null)
        {
            return NotFound(new { message = "Session not found or expired" });
        }

        return Ok(new
        {
            sessionId = session.SessionId,
            status = session.Status.ToString(),
            confirmedAt = session.ConfirmedAt,
            userInfo = session.UserInfo
        });
    }

    [HttpPost("confirm")]
    public IActionResult ConfirmLogin([FromBody] ConfirmLoginRequest request)
    {
        var success = _sessionService.ConfirmLogin(request.SessionId, request.UserInfo);
        
        if (!success)
        {
            return BadRequest(new { message = "Invalid or expired session" });
        }

        return Ok(new { message = "Login confirmed successfully" });
    }
}

public class ConfirmLoginRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string? UserInfo { get; set; }
}

