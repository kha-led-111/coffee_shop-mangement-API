using CoffeeShopAPI.DTOs;
using CoffeeShopAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoffeeShopAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>POST /api/auth/login — Returns JWT token</summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Invalid email or password."));

        return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successful."));
    }

    /// <summary>POST /api/auth/register — Admin only</summary>
    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (result == null)
            return BadRequest(ApiResponse<UserDto>.Fail("Email already in use."));

        return Ok(ApiResponse<UserDto>.Ok(result, "User created successfully."));
    }

    /// <summary>GET /api/auth/me — Returns current user profile</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user   = await _authService.GetUserByIdAsync(userId);
        if (user == null) return NotFound(ApiResponse<UserDto>.Fail("User not found."));

        return Ok(ApiResponse<UserDto>.Ok(user));
    }
}
