using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Attributes;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.Chat;
using AiIncubator.Server.Services.Chat.Sessions;

namespace AiIncubator.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/chat/")]
[ControllerName(name: "chat")]
public class ChatController(IChatService chatService) : ControllerBase
{
    #region Public methods region

    /// <summary>Creates a new chat session.</summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST api/chat/sessions
    ///     {
    ///       "title": "Questions about onboarding"
    ///     }
    /// </remarks>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ApiResponse<ChatSession>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<ChatSession>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        ChatSession session = await chatService.CreateSessionAsync(request.Title, cancellationToken);
        return ApiResponse<ChatSession>.Ok(session);
    }

    /// <summary>Lists chat sessions, most recent first.</summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChatSession>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<IReadOnlyList<ChatSession>>> ListSessions(CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatSession> sessions = await chatService.ListSessionsAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<ChatSession>>.Ok(sessions);
    }

    /// <summary>Gets a chat session with its full message history.</summary>
    [HttpGet("sessions/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ChatSession>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<ChatSession>> GetSession(string id, CancellationToken cancellationToken)
    {
        ChatSession session = await chatService.GetSessionAsync(id, cancellationToken);
        return ApiResponse<ChatSession>.Ok(session);
    }

    /// <summary>Sends a message and returns the assistant's answer with its sources.</summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST api/chat/sessions/{id}/messages
    ///     {
    ///       "message": "What is the refund policy?",
    ///       "topK": 4
    ///     }
    /// </remarks>
    [HttpPost("sessions/{id}/messages")]
    [ProducesResponseType(typeof(ApiResponse<ChatMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<ChatMessage>> SendMessage(
        string id,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        ChatMessage reply = await chatService.SendMessageAsync(id, request.Message, request.TopK, cancellationToken);
        return ApiResponse<ChatMessage>.Ok(reply);
    }

    #endregion
}
