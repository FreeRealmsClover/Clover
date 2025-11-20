using System.ComponentModel.DataAnnotations;

namespace Sanctuary.WebAPI.Models;

public class LoginRequestModel
{
    [Required(ErrorMessage = "Username is required.")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    public required string Password { get; set; }
}