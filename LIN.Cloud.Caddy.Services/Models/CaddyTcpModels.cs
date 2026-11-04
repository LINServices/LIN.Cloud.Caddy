using System.Text.Json.Serialization;

namespace LIN.Cloud.Caddy.Services.Models;

/// <summary>
/// Server del módulo layer4 (caddy-l4) de Caddy.
/// </summary>
public class CaddyTcpServer
{
    [JsonPropertyName("listen")]
    public List<string> Listen { get; set; } = new();

    [JsonPropertyName("routes")]
    public List<CaddyTcpRoute> Routes { get; set; } = new();
}

public class CaddyTcpRoute
{
    [JsonPropertyName("match")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CaddyTcpMatch>? Match { get; set; }

    [JsonPropertyName("handle")]
    public List<CaddyTcpHandle> Handle { get; set; } = new();
}

public class CaddyTcpMatch
{
    [JsonPropertyName("tls")]
    public CaddyTcpTlsMatch Tls { get; set; } = new();
}

public class CaddyTcpTlsMatch
{
    [JsonPropertyName("sni")]
    public List<string> Sni { get; set; } = new();
}

public class CaddyTcpHandle
{
    [JsonPropertyName("handler")]
    public string Handler { get; set; } = "proxy";

    [JsonPropertyName("upstreams")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CaddyTcpUpstream>? Upstreams { get; set; }
}

public class CaddyTcpUpstream
{
    [JsonPropertyName("dial")]
    public List<string> Dial { get; set; } = new();
}
