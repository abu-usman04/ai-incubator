using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AiIncubator.Server.Common;
using AiIncubator.Server.Common.Attributes;
using AiIncubator.Server.Models.Domain;
using AiIncubator.Server.Models.RequestModels.Quiz;
using AiIncubator.Server.Services.Quizzes;

namespace AiIncubator.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/quizzes/")]
[ControllerName(name: "quizzes")]
public class QuizController(IQuizService quizService) : ControllerBase
{
    #region Public methods region

    /// <summary>Generates a grounded quiz from a topic or knowledge module.</summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST api/quizzes
    ///     {
    ///       "topic": "onboarding",
    ///       "questionCount": 5,
    ///       "includeShortAnswer": true
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Quiz>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<Quiz>> Generate(
        [FromBody] GenerateQuizRequest request,
        CancellationToken cancellationToken)
    {
        Quiz quiz = await quizService.GenerateAsync(request, cancellationToken);
        return ApiResponse<Quiz>.Ok(quiz);
    }

    /// <summary>Lists generated quizzes, most recent first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Quiz>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<PagedResult<Quiz>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<Quiz> result = await quizService.ListAsync(page, pageSize, cancellationToken);
        return ApiResponse<PagedResult<Quiz>>.Ok(result);
    }

    /// <summary>Gets a quiz with its questions.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Quiz>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<Quiz>> Get(string id, CancellationToken cancellationToken)
    {
        Quiz quiz = await quizService.GetAsync(id, cancellationToken);
        return ApiResponse<Quiz>.Ok(quiz);
    }

    /// <summary>Starts a new attempt for a quiz.</summary>
    [HttpPost("{id}/attempts")]
    [ProducesResponseType(typeof(ApiResponse<QuizAttempt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<QuizAttempt>> StartAttempt(string id, CancellationToken cancellationToken)
    {
        QuizAttempt attempt = await quizService.StartAttemptAsync(id, cancellationToken);
        return ApiResponse<QuizAttempt>.Ok(attempt);
    }

    /// <summary>Submits one answer and returns the updated attempt with live score.</summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST api/quizzes/attempts/{attemptId}/answers
    ///     {
    ///       "questionId": "abc",
    ///       "selectedOptionIndex": 0
    ///     }
    /// </remarks>
    [HttpPost("attempts/{attemptId}/answers")]
    [ProducesResponseType(typeof(ApiResponse<QuizAttempt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<QuizAttempt>> SubmitAnswer(
        string attemptId,
        [FromBody] SubmitAnswerRequest request,
        CancellationToken cancellationToken)
    {
        QuizAttempt attempt = await quizService.SubmitAnswerAsync(attemptId, request, cancellationToken);
        return ApiResponse<QuizAttempt>.Ok(attempt);
    }

    /// <summary>Gets an attempt with its answers and current score.</summary>
    [HttpGet("attempts/{attemptId}")]
    [ProducesResponseType(typeof(ApiResponse<QuizAttempt>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<QuizAttempt>> GetAttempt(string attemptId, CancellationToken cancellationToken)
    {
        QuizAttempt attempt = await quizService.GetAttemptAsync(attemptId, cancellationToken);
        return ApiResponse<QuizAttempt>.Ok(attempt);
    }

    #endregion
}
