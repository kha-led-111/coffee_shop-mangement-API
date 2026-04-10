using CoffeeShopAPI.Data;
using CoffeeShopAPI.DTOs;
using CoffeeShopAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoffeeShopAPI.Services;

// ── Token Service ─────────────────────────────────────────────────────────────
public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public string GenerateToken(User user)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry  = int.Parse(_config["Jwt:ExpiryInMinutes"] ?? "1440");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name,               user.Name),
            new Claim(ClaimTypes.Role,               user.Role),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ── Auth Service ──────────────────────────────────────────────────────────────
public class AuthService : IAuthService
{
    private readonly AppDbContext  _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, ITokenService tokenService, IConfiguration config)
    {
        _db           = db;
        _tokenService = tokenService;
        _config       = config;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return null;

        var token  = _tokenService.GenerateToken(user);
        var expiry = int.Parse(_config["Jwt:ExpiryInMinutes"] ?? "1440");

        return new AuthResponse
        {
            Token     = token,
            TokenType = "Bearer",
            ExpiresIn = expiry * 60,
            User      = MapToDto(user)
        };
    }

    public async Task<UserDto?> RegisterAsync(RegisterRequest request)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (exists) return null;

        var user = new User
        {
            Name     = request.Name,
            Email    = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role     = request.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? null : MapToDto(user);
    }

    private static UserDto MapToDto(User u) =>
        new() { Id = u.Id, Name = u.Name, Email = u.Email, Role = u.Role };
}
