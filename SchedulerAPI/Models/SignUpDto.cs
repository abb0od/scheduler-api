using System.ComponentModel.DataAnnotations;

namespace SchedulerAPI.Models;

public class SignUpDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public UserType Type { get; set; } = UserType.Normal;
}
