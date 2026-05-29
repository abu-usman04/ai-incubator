namespace AiIncubator.Server.Tests.Services.Chat;

using FluentAssertions;
using Moq;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Common.Exceptions;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Chat.Sessions;
using AiIncubator.Server.Services.Retrieval;

public class ChatServiceTests
{
    private readonly Mock<IQueryService> _query = new();
    private readonly InMemoryChatSessionStore _store = new();
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        _service = new ChatService(_store, _query.Object);
    }

    [Fact]
    public async Task SendMessageAsync_AppendsUserAndAssistantMessages()
    {
        var sources = new[] { new RetrievedChunk("doc#0", "ctx", 0.8f, new Dictionary<string, string>()) };
        _query
            .Setup(s => s.AnswerAsync("hi", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RagAnswer("hello", sources));
        ChatSession session = await _service.CreateSessionAsync(null, CancellationToken.None);

        ChatMessage reply = await _service.SendMessageAsync(session.Id, "hi", 4, CancellationToken.None);

        reply.Role.Should().Be(ChatRole.Assistant);
        reply.Content.Should().Be("hello");
        reply.Sources.Should().HaveCount(1);

        ChatSession reloaded = await _service.GetSessionAsync(session.Id, CancellationToken.None);
        reloaded.Messages.Should().HaveCount(2);
        reloaded.Messages[0].Role.Should().Be(ChatRole.User);
    }

    [Fact]
    public async Task SendMessageAsync_UnknownSession_ThrowsNotFound()
    {
        Func<Task> act = () => _service.SendMessageAsync("missing", "hi", 4, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task SendMessageAsync_BlankMessage_ThrowsBadRequest()
    {
        ChatSession session = await _service.CreateSessionAsync(null, CancellationToken.None);

        Func<Task> act = () => _service.SendMessageAsync(session.Id, "  ", 4, CancellationToken.None);

        (await act.Should().ThrowAsync<AppException>()).Which.ErrorCode.Should().Be("BAD_REQUEST");
    }
}
