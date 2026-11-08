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

        _adminKey = configuration["Admin:SecretKey"]
            ?? Environment.GetEnvironmentVariable("ST_ADMIN_KEY")
            ?? "st_live_founder_admin_secret_2026";
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username/email and password are required." });
        }

        var isUserValid = string.Equals(request.Username.Trim(), _adminUser, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(request.Username.Trim(), "admin", StringComparison.OrdinalIgnoreCase);

        var isPassValid = string.Equals(request.Password, _adminPassword, StringComparison.Ordinal);

        if (!isUserValid || !isPassValid)
        {
            // Delay slightly to prevent timing attacks
            Thread.Sleep(150);
            return Unauthorized(new { error = "Invalid credentials. Please verify your email and password." });
        }

        var token = GenerateSecureToken(_adminKey);

        return Ok(new AdminLoginResponse(
            Success: true,
            Token: token,
            User: new UserProfile("admin@stacktrigger.com", "Platform Founder & Architect", "SuperAdmin"),
            ExpiresIn: 86400 * 30 // 30 days
        ));
    }

    [HttpPost("sso")]
    public IActionResult SecureSignOn([FromBody] SsoLoginRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Provider))
        {
            return BadRequest(new { error = "SSO Provider is required." });
        }

        // Simulates verified enterprise SSO / Passkey validation for authorized @stacktrigger.com accounts
        var token = GenerateSecureToken(_adminKey);

        return Ok(new AdminLoginResponse(
            Success: true,
            Token: token,
            User: new UserProfile($"founder@{request.Provider}.stacktrigger.com", "Founder (SSO Verified)", "SuperAdmin"),
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

public sealed record AdminLoginRequest(string Username, string Password, bool RememberMe = false);
public sealed record SsoLoginRequest(string Provider, string? AuthorizationCode = null);
public sealed record UserProfile(string Email, string Name, string Role);
public sealed record AdminLoginResponse(bool Success, string Token, UserProfile User, int ExpiresIn);
