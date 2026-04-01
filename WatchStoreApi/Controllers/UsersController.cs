using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using WatchStoreApi.Data;
using WatchStoreApi.Models;

namespace WatchStoreApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly ApiDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public UsersController(ApiDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        var existingUser = _dbContext.Users.FirstOrDefault(u => u.Email == user.Email);
        if (existingUser != null)
        {
            return BadRequest("Email already in use");
        }

        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash = passwordHasher.HashPassword(user, user.PasswordHash);

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var currentUser = _dbContext.Users.FirstOrDefault(u => u.Email == request.Email);
        if (currentUser == null)
        {
            return NotFound("User not found");
        }

        var passwordHasher = new PasswordHasher<User>();
        var result = passwordHasher.VerifyHashedPassword(currentUser, currentUser.PasswordHash, request.Password);
        if (result != PasswordVerificationResult.Success)
        {
            return Unauthorized("Invalid password");
        }

        //生成JWT Token
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        //添加Claims
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, request.Email),
            new Claim(ClaimTypes.Role, currentUser.Role)
        };
        var token = new JwtSecurityToken(issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials);
        return new ObjectResult(new
        {
            access_token = new JwtSecurityTokenHandler().WriteToken(token),
            token_type = "Bearer",
            user_id = currentUser.Id,
            user_name = currentUser.Name,
        });
    }
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}
