using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    // Endpoint для аутентификации пользователя
    // POST /mqtt/auth
    [HttpPost("auth")]
    public async Task<IActionResult> Authenticate([FromBody] AuthRequest request)
    {
        try
        {
            _logger.LogInformation($"Auth request for user: {request.Username}");
            bool isAuthenticated = false;

            var response = new AuthResponse
            {
                Ok = isAuthenticated,
                Error = ""
            };

            var user = await _userManager.FindByNameAsync(request.Username);

            if (user == null)
            {
                _logger.LogInformation($"User: {request.Username} not found");
                response.Error = "User not found";
            }
            response.Ok = (await _signInManager.CheckPasswordSignInAsync(user, request.Password, false)).Succeeded;

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error");
            return StatusCode(500, new { result = false });
        }
    }

    // Endpoint для проверки ACL (прав доступа)
    // POST /mqtt/acl
    [HttpPost("acl")]
    public async Task<IActionResult> CheckAcl([FromBody] AclRequest request)
    {
        try
        {
            _logger.LogInformation($"ACL request for user: {request.Username}, topic: {request.Topic}");
            return Ok(new { result = request.Topic.StartsWith(request.Username + "/") || request.Username.ToUpper()=="ADMIN" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ACL check error");
            return StatusCode(500, new { result = false });
        }
    }

    // Endpoint для проверки суперпользователя
    // POST /mqtt/superuser
    [HttpPost("superuser")]
    public async Task<IActionResult> CheckSuperuser([FromBody] AuthRequest request)
    {
        try
        {
            _logger.LogInformation($"Superuser check for user: {request.Username}");

            return Ok(new { result = false }); //Нет у нас суперюзеров
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Superuser check error");
            return StatusCode(500, new { result = false });
        }
    }

    // Health check endpoint
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}