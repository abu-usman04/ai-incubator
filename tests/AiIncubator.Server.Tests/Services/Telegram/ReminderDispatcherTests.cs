namespace AiIncubator.Server.Tests.Services.Telegram;

using FluentAssertions;
using Moq;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.LearningPlans;
using AiIncubator.Server.Services.Telegram;

public class ReminderDispatcherTests
{
    private readonly Mock<ILearningPlanRepository> _plans = new();
    private readonly Mock<ITelegramChatLinkRepository> _links = new();
    private readonly Mock<ITelegramService> _telegram = new();
    private readonly ReminderDispatcher _dispatcher;
    private static readonly DateTime Now = new(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);

    public ReminderDispatcherTests()
    {
        _dispatcher = new ReminderDispatcher(_plans.Object, _links.Object, _telegram.Object);
    }

    [Fact]
    public async Task DispatchDueRemindersAsync_LinkedUser_SendsOneMessage()
    {
        StubDue("dev-user", "task-1");
        _links.Setup(l => l.GetChatIdsForUserAsync("dev-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new long[] { 555 });

        int sent = await _dispatcher.DispatchDueRemindersAsync(Now, CancellationToken.None);

        sent.Should().Be(1);
        _telegram.Verify(t => t.SendMessageAsync(555, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchDueRemindersAsync_NoLinkedChat_SendsNothing()
    {
        StubDue("dev-user", "task-1");
        _links.Setup(l => l.GetChatIdsForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<long>());

        int sent = await _dispatcher.DispatchDueRemindersAsync(Now, CancellationToken.None);

        sent.Should().Be(0);
        _telegram.Verify(t => t.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchDueRemindersAsync_AlreadyReminded_NotResent()
    {
        StubDue("dev-user", "task-1");
        _links.Setup(l => l.GetChatIdsForUserAsync("dev-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new long[] { 555 });

        await _dispatcher.DispatchDueRemindersAsync(Now, CancellationToken.None);
        await _dispatcher.DispatchDueRemindersAsync(Now, CancellationToken.None);

        _telegram.Verify(t => t.SendMessageAsync(555, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private void StubDue(string userId, string taskId)
    {
        _plans.Setup(p => p.ListDueTasksAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new DuePlanTask
                {
                    PlanId = "plan-1",
                    UserId = userId,
                    Task = new PlanTask { Id = taskId, Type = PlanTaskType.Read, Title = "Read handbook" }
                }
            });
    }
}
