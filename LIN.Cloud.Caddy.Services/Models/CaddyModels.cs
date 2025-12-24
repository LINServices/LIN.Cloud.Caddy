using System.Text.Json.Serialization;

namespace LIN.Cloud.Caddy.Services.Models;

public class CaddyRoute
{
    [JsonPropertyName("@id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("match")]
    public List<CaddyMatch> Match { get; set; } = new();

    [JsonPropertyName("handle")]
    public List<CaddyHandle> Handle { get; set; } = new();
}

public class CaddyMatch
{
    [JsonPropertyName("host")]
    public List<string> Host { get; set; } = new();
}

public class CaddyHandle
{
    [JsonPropertyName("handler")]
    public string Handler { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CaddyResponse? Response { get; set; }

    [JsonPropertyName("upstreams")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CaddyUpstream>? Upstreams { get; set; }

    [JsonPropertyName("headers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CaddyHeaders? Headers { get; set; }
}

public class CaddyResponse
{
    [JsonPropertyName("set")]
    public Dictionary<string, List<string>> Set { get; set; } = new();

    [JsonPropertyName("delete")]
    public List<string> Delete { get; set; } = new();

    [JsonPropertyName("deferred")]
    public bool Deferred { get; set; }
}

public class CaddyUpstream
{
    [JsonPropertyName("dial")]
    public string Dial { get; set; } = string.Empty;
}

public class CaddyHeaders
{
    [JsonPropertyName("request")]
    public CaddyHeaderAction? Request { get; set; }

    [JsonPropertyName("response")]
    public CaddyHeaderAction? Response { get; set; }
}

public class CaddyHeaderAction
{
    [JsonPropertyName("delete")]
    public List<string> Delete { get; set; } = new();
}
