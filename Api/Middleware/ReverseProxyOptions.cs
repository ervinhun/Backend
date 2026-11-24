using Microsoft.AspNetCore.HttpOverrides;

namespace Api.Middleware;

/// <summary>Bound from "ReverseProxy" (env: ReverseProxy__KnownNetworks__0, ...).</summary>
public class ReverseProxyOptions
{
    public const string SectionName = "ReverseProxy";

    /// <summary>
    /// Where the proxy connects from, as CIDR ("10.0.0.0/8"). Empty when there is no proxy: the header is then ignored,
    /// as anyone could send it to get another IP in the rate limits.
    /// </summary>
    public string[] KnownNetworks { get; set; } = [];

    /// <summary>The header the proxy puts the client's IP in.</summary>
    public string ClientIpHeader { get; set; } = ForwardedHeadersDefaults.XForwardedForHeaderName;

    public void Apply(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        options.ForwardedForHeaderName = ClientIpHeader;

        // Only the configured ones, not the loopback that is trusted by default.
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        foreach (var network in KnownNetworks)
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
    }
}
