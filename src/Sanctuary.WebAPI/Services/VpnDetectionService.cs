using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Sanctuary.WebAPI.Options;

namespace Sanctuary.WebAPI.Services;

public sealed class VpnDetectionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VpnDetectionService> _logger;
    private readonly IOptions<VpnApiOptions> _options;

    public VpnDetectionService(HttpClient httpClient, ILogger<VpnDetectionService> logger, IOptions<VpnApiOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;
    }

    // Returns true only when vpnapi.io positively identifies the address as
    // a VPN, proxy, Tor exit node, or cloud relay. Fails OPEN (false) on
    // any problem talking to the API, a missing key, or a private/loopback
    // address - an outage on their end should never lock every real player
    // out of login/registration.
    public async Task<bool> IsVpnOrProxyAsync(string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        if (string.IsNullOrWhiteSpace(_options.Value.ApiKey))
        {
            _logger.LogWarning("VpnApi API key is not configured - VPN/proxy detection is disabled.");
            return false;
        }

        if (IPAddress.TryParse(ipAddress, out var parsed) && (IPAddress.IsLoopback(parsed) || IsPrivate(parsed)))
            return false;

        try
        {
            var url = $"https://vpnapi.io/api/{Uri.EscapeDataString(ipAddress)}?key={_options.Value.ApiKey}";

            var result = await _httpClient.GetFromJsonAsync<VpnApiResponse>(url, cancellationToken);

            if (result?.Security is null)
            {
                _logger.LogWarning("VpnApi lookup returned no security data for {IpAddress}. Message: {Message}", ipAddress, result?.Message);
                return false;
            }

            return result.Security.Vpn || result.Security.Proxy || result.Security.Tor || result.Security.Relay;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "VpnApi lookup threw for {IpAddress}", ipAddress);
            return false;
        }
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;

        var bytes = address.GetAddressBytes();

        return bytes[0] switch
        {
            10 => true,
            172 => bytes[1] is >= 16 and <= 31,
            192 => bytes[1] == 168,
            _ => false,
        };
    }

    private sealed class VpnApiResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("security")]
        public VpnApiSecurity? Security { get; set; }
    }

    private sealed class VpnApiSecurity
    {
        [JsonPropertyName("vpn")]
        public bool Vpn { get; set; }

        [JsonPropertyName("proxy")]
        public bool Proxy { get; set; }

        [JsonPropertyName("tor")]
        public bool Tor { get; set; }

        [JsonPropertyName("relay")]
        public bool Relay { get; set; }
    }
}
