using System.ComponentModel.DataAnnotations;

namespace Sanctuary.WebAPI.Models;

public class ForgotPasswordRequestModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    public required string Email { get; set; }
}
