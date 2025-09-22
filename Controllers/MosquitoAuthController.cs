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

            var user = await _userManager.FindByNameAsync(request.Username);

            if (user == null)
            {
                _logger.LogInformation($"User: {request.Username} not found");
                response.Error = "User not found";
                return NotFound(response);
            }

            response.Ok = (await _signInManager.CheckPasswordSignInAsync(user, request.Password, false)).Succeeded;

            if (!response.Ok)
            {
                response.Error = "Invalid password";
                _logger.LogInformation($"Invalid password for user: {request.Username}");
            }

            return Unauthorized(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error");
            return StatusCode(500, new { result = false });
        }
    }
}