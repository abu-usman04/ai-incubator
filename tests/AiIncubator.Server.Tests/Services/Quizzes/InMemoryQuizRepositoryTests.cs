namespace AiIncubator.Server.Tests.Services.Quizzes;

using FluentAssertions;
using AiIncubator.Server.Common.Enums;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Services.Quizzes;

public class InMemoryQuizRepositoryTests
{
    private readonly InMemoryQuizRepository _repository = new();

    [Fact]
    public async Task ListAsync_OrdersByCreatedAtDesc_AndPaginates()
    {
        await _repository.AddAsync(Build("a", new DateTime(2026, 1, 1)), CancellationToken.None);
        await _repository.AddAsync(Build("b", new DateTime(2026, 1, 2)), CancellationToken.None);
        await _repository.AddAsync(Build("c", new DateTime(2026, 1, 3)), CancellationToken.None);

        IReadOnlyList<Quiz> page = await _repository.ListAsync(page: 1, pageSize: 2, CancellationToken.None);

        page.Should().HaveCount(2);
        page[0].Id.Should().Be("c");
        page[1].Id.Should().Be("b");
        (await _repository.CountAsync(CancellationToken.None)).Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_Unknown_ReturnsNull()
    {
        (await _repository.GetByIdAsync("missing", CancellationToken.None)).Should().BeNull();
    }

    private static Quiz Build(string id, DateTime createdAt) => new()
    {
        Id = id,
        Title = $"Quiz {id}",
        Topic = "topic",
        Status = QuizStatus.Ready,
        Questions = [],
        CreatedAt = createdAt
    };
}
