namespace Sanctuary.WebAPI.Models;

public record class LoginResponseModel
{
    public required string SessionId { get; set; }
    public string? LaunchArguments { get; set; }
}