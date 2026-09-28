using System.ComponentModel.DataAnnotations;

namespace Sanctuary.WebAPI.Models;

public class ResendConfirmationRequestModel
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters long.")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Username can only contain letters, numbers, underscores, and dots.")]
    public required string Username { get; set; }
}
