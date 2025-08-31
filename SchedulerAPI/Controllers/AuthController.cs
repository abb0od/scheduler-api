using Microsoft.AspNetCore.Mvc;
using SchedulerAPI.Models;
using SchedulerAPI.Services;

namespace SchedulerAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AuthService _authService;

    public AuthController(UserService userService, AuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpDto dto)
    {
        var existingUser = await _userService.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            return Conflict("Email already in use.");

        var hashedPassword = _authService.HashPassword(dto.Password);

        var newUser = new User
        {
            Email = dto.Email,
            PasswordHash = hashedPassword,
            FullName = dto.FullName,
            Type = dto.Type
        };

        await _userService.CreateAsync(newUser);

        var token = _authService.GenerateJweToken(newUser);
        return Ok(new { token });
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInDto dto)
    {
        var user = await _userService.GetByEmailAsync(dto.Email);
        if (user == null || !_authService.VerifyPassword(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        var token = _authService.GenerateJweToken(user);
        return Ok(new { token });
    }
}
