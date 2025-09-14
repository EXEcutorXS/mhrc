using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace mhrc;

[ApiController]
[Route("mqtt")]
public class MosquitoAuthController : ControllerBase
{

    private readonly ILogger<MosquitoAuthController> _logger;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    public MosquitoAuthController(SignInManager<IdentityUser> signInManager,
                         UserManager<IdentityUser> userManager, ILogger<MosquitoAuthController> logger)
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpPost("auth")]
    public async Task<IActionResult> Authenticate([FromBody] AuthRequest request)
    {
        try
        {
            _logger.LogInformation($"Auth request for user: {request.Username}");

            var response = new AuthResponse
            {
                Ok = false,
                Error = ""
            };

            // ѕровер€ем, €вл€етс€ ли переданный "пароль" JWT токеном
            if (IsJwtToken(request.Password))
            {
                _logger.LogInformation($"JWT token authentication detected for user: {request.Username}");
                return await AuthenticateWithToken(request.Username, request.Password, response);
            }

            // —тандартна€ аутентификаци€ по паролю
            _logger.LogInformation($"Password authentication for user: {request.Username}");
            return await AuthenticateWithPassword(request.Username, request.Password, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error");
            return StatusCode(500, new { result = false });
        }
    }

    // ћетод дл€ проверки, €вл€етс€ ли строка JWT токеном
    private bool IsJwtToken(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // JWT токен состоит из трех частей, разделенных точками
        var parts = input.Split('.');
        if (parts.Length != 3)
            return false;

        try
        {
            // ѕытаемс€ декодировать header токена
            var header = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0].PadRight(parts[0].Length + (4 - parts[0].Length % 4) % 4, '=')));
            return header.Contains("alg") && header.Contains("typ"); // ѕровер€ем наличие стандартных полей JWT
        }
        catch
        {
            return false;
        }
    }

    private async Task<IActionResult> AuthenticateWithPassword(string username, string password, AuthResponse response)
    {
        var user = await _userManager.FindByNameAsync(username);

        if (user == null)
        {
            _logger.LogInformation($"User: {username} not found");
            response.Error = "User not found";
            return Ok(response);
        }

        response.Ok = (await _signInManager.CheckPasswordSignInAsync(user, password, false)).Succeeded;

        if (!response.Ok)
        {
            response.Error = "Invalid password";
            _logger.LogInformation($"Invalid password for user: {username}");
        }

        return Ok(response);
    }

    private async Task<IActionResult> AuthenticateWithToken(string username, string token, AuthResponse response)
    {
        try
        {
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var jwtSettings = configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var tokenUsername = principal.FindFirst(ClaimTypes.Name)?.Value;
            if (tokenUsername != username)
            {
                response.Error = "Token does not match username";
                _logger.LogInformation($"Token username mismatch: expected {username}, got {tokenUsername}");
                return Ok(response);
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                response.Error = "User not found";
                _logger.LogInformation($"User: {username} not found");
                return Ok(response);
            }

            response.Ok = true;
            _logger.LogInformation($"Token authentication successful for user: {username}");
            return Ok(response);
        }
        catch (SecurityTokenExpiredException)
        {
            response.Error = "Token expired";
            _logger.LogInformation($"Expired token for user: {username}");
            return Ok(response);
        }
        catch (SecurityTokenException ex)
        {
            response.Error = "Invalid token";
            _logger.LogInformation($"Invalid token for user: {username}, error: {ex.Message}");
            return Ok(response);
        }
        catch (Exception ex)
        {
            response.Error = "Token validation error";
            _logger.LogError(ex, $"Token validation error for user: {username}");
            return Ok(response);
        }
    }

    // Endpoint дл€ проверки ACL (прав доступа)
    // POST /mqtt/acl
    [HttpPost("acl")]
    public async Task<IActionResult> CheckAcl([FromBody] AclRequest request)
    {
        var response = new AuthResponse() { Ok = request.Topic.StartsWith(request.Username + "/") || request.Username.ToUpper() == "ADMIN", Error = "" };
        try
        {
            _logger.LogInformation($"ACL request for user: {request.Username}, topic: {request.Topic}");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ACL check error");
            return StatusCode(500, new { result = false });
        }
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}