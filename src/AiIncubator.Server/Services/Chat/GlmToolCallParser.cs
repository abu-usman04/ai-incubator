using System.Text.Json;
using AiIncubator.Server.Models.Glm;

namespace AiIncubator.Server.Services.Chat;

/// <summary>
/// Deserializes the JSON arguments of a GLM tool/function call into a typed object.
/// Returns <c>null</c> when no matching tool call is present or the arguments do not parse,
/// leaving the throw-or-fallback decision to the caller.
/// </summary>
public static class GlmToolCallParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T? Parse<T>(GlmChatResponse response, string functionName) where T : class
    {
        List<GlmToolCall>? toolCalls = response.Choices.FirstOrDefault()?.Message.ToolCalls;
        GlmToolCall? call =
            toolCalls?.FirstOrDefault(tool => tool.Function.Name == functionName)
            ?? toolCalls?.FirstOrDefault();

        if (call is null || string.IsNullOrWhiteSpace(call.Function.Arguments))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(call.Function.Arguments, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
