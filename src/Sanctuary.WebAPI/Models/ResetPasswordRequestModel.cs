using System.ComponentModel.DataAnnotations;

namespace Sanctuary.WebAPI.Models;

public class ResetPasswordRequestModel
{
    [Required(ErrorMessage = "Token is required.")]
    public required string Token { get; set; }

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters long.")]
    [RegularExpression(@"^[\x00-\x7F]+$", ErrorMessage = "Password can only contain ASCII characters.")]
    public required string NewPassword { get; set; }
}
