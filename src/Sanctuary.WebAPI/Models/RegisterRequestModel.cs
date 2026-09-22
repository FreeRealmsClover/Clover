using System.ComponentModel.DataAnnotations;

namespace Sanctuary.WebAPI.Models;

public class RegisterRequestModel
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters long.")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Username can only contain letters, numbers, underscores, and dots.")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters long.")]
    [RegularExpression(@"^[\x00-\x7F]+$", ErrorMessage = "Password can only contain ASCII characters.")]
    public required string Password { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    [StringLength(320, ErrorMessage = "Email must be at most 320 characters long.")]
    public required string Email { get; set; }
}