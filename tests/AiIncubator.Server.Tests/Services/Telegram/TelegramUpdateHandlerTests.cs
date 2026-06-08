namespace AiIncubator.Server.Tests.Services.Telegram;

using FluentAssertions;
using Moq;
using AiIncubator.Server.Services.Telegram;

public class TelegramUpdateHandlerTests
{
    private readonly Mock<ITelegramQuizConversation> _quiz = new();
    private readonly Mock<ITelegramChatLinkRepository> _links = new();
    private readonly Mock<ITelegramService> _telegram = new();
    private readonly TelegramUpdateHandler _handler;

    public TelegramUpdateHandlerTests()
    {
        _handler = new TelegramUpdateHandler(_quiz.Object, _links.Object, _telegram.Object);
    }

    [Fact]
    public async Task QuizCommand_RoutesToQuizConversation()
    {
        _quiz.Setup(q => q.HandleAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _handler.HandleAsync(100, "/quiz quiz-1", CancellationToken.None);

        _quiz.Verify(q => q.HandleAsync(100, "/quiz quiz-1", It.IsAny<CancellationToken>()), Times.Once);
        _telegram.Verify(t => t.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkCommand_LinksChatToUser()
    {
        await _handler.HandleAsync(100, "/link dev-user", CancellationToken.None);

        _links.Verify(l => l.LinkAsync(100, "dev-user", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnknownInput_SendsHelp()
    {
        await _handler.HandleAsync(100, "hello", CancellationToken.None);

        _telegram.Verify(
            t => t.SendMessageAsync(100, It.Is<string>(text => text.Contains("Commands:")), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
