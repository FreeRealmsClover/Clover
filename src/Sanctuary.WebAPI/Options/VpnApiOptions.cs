namespace Sanctuary.WebAPI.Options;

public class VpnApiOptions
{
    public const string Section = "VpnApi";

    // Intentionally not [Required]/ValidateOnStart - the app should still
    // boot with this blank (before the key is obtained), it just means
    // VpnDetectionService logs a warning and skips the check.
    public string? ApiKey { get; set; }
}
