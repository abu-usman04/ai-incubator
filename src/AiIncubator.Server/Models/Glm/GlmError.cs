using System.Text.Json.Serialization;

namespace AiIncubator.Server.Models.Glm;

/// <summary>
/// Error information returned by the ZhipuAI API.
/// Present either in the HTTP response body on failure, or within a 200 OK response
/// for business-level errors (e.g., content policy violations, rate limits).
/// </summary>
public class GlmError
{
    /// <summary>
    /// Error code identifying the type of failure.
    /// Common codes: 1000-1004 (auth), 1113 (insufficient balance),
    /// 1211 (model doesn't exist), 1261 (prompt too long),
    /// 1301 (sensitive content), 1302-1305 (rate limits).
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>Human-readable error message describing the failure.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
