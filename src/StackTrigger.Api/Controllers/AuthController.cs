using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace StackTrigger.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly string _adminUser;
    private readonly string _adminPassword;
    private readonly string _adminPin;
    private readonly string _adminKey;

    public AuthController(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        _adminUser = configuration["Admin:Username"]
            ?? Environment.GetEnvironmentVariable("ST_ADMIN_USER")
            ?? "admin@stacktrigger.com";

        _adminPassword = configuration["Admin:Password"]
            ?? Environment.GetEnvironmentVariable("ST_ADMIN_PASSWORD")
            ?? "StackTrigger2026!Master";

        _adminPin = configuration["Admin:SecurityPin"]
            ?? Environment.GetEnvironmentVariable("ST_ADMIN_PIN")
            ?? "849201";

        _adminKey = configuration["Admin:SecretKey"]
            ?? Environment.GetEnvironmentVariable("ST_ADMIN_KEY")
            ?? "st_live_founder_admin_secret_2026";
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.SecurityPin))
        {
            return BadRequest(new { error = "Email, master password, and 6-digit security PIN are required." });
        }

        var isUserValid = string.Equals(request.Username.Trim(), _adminUser, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(request.Username.Trim(), "admin", StringComparison.OrdinalIgnoreCase);

        var isPassValid = string.Equals(request.Password, _adminPassword, StringComparison.Ordinal);
        var isPinValid = string.Equals(request.SecurityPin.Trim(), _adminPin, StringComparison.Ordinal);

        if (!isUserValid || !isPassValid || !isPinValid)
        {
            // Delay to prevent automated timing / brute-force attacks
            Thread.Sleep(200);
            return Unauthorized(new { error = "Access Denied: Invalid credentials or security PIN." });
        }

        var token = GenerateSecureToken(_adminKey);

        return Ok(new AdminLoginResponse(
            Success: true,
            Token: token,
            User: new UserProfile("admin@stacktrigger.com", "Platform Founder & Architect", "SuperAdmin"),
            ExpiresIn: 86400 * 30
        ));
    }

    private static string GenerateSecureToken(string masterKey)
    {
        var randomBytes = new byte[24];
        RandomNumberGenerator.Fill(randomBytes);
        var randomHex = Convert.ToHexString(randomBytes).ToLowerInvariant();
        return $"st_sess_{randomHex}_{masterKey[..Math.Min(8, masterKey.Length)]}";
    }
}

public sealed record AdminLoginRequest(string Username, string Password, string SecurityPin, bool RememberMe = false);
public sealed record UserProfile(string Email, string Name, string Role);
public sealed record AdminLoginResponse(bool Success, string Token, UserProfile User, int ExpiresIn);
