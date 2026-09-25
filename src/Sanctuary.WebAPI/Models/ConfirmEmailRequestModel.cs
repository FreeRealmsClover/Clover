using System.ComponentModel.DataAnnotations;

namespace Sanctuary.WebAPI.Models;

public class ConfirmEmailRequestModel
{
    [Required(ErrorMessage = "Token is required.")]
    public required string Token { get; set; }
}
