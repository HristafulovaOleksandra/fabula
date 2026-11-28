using System.ComponentModel.DataAnnotations;
using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Fabula.Api.Placement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

public record PlacementOptionDto(int Id, string Text);

public record PlacementQuestionDto(int Id, string Instruction, string Text, IReadOnlyList<PlacementOptionDto> Options);

public record PlacementTestDto(IReadOnlyList<PlacementQuestionDto> Questions, int MinTopics, int MaxTopics);

public record PlacementAnswer(int QuestionId, int Choice);

public record PlacementSubmitRequest([MaxLength(100)] IReadOnlyList<PlacementAnswer> Answers);

public record PlacementResultDto(CefrLevel Level, IReadOnlyList<LevelScore> Levels, UserDto User);

public record FavoriteTopicsRequest([MaxLength(10)] IReadOnlyList<string> Topics);

[ApiController]
[Authorize]
[Route("api")]
public class PlacementController(FabulaDbContext db, PlacementTest test) : ControllerBase
{
    private const int MinTopics = 3;
    private const int MaxTopics = 5;

    [HttpGet("placement")]
    public ActionResult<PlacementTestDto> Get() =>
        Ok(new PlacementTestDto(
            test.Draw(Random.Shared)
                .Select(q => new PlacementQuestionDto(
                    q.Id,
                    q.Instruction,
                    q.Text,
                    q.Options.Select((text, index) => new PlacementOptionDto(index, text)).OrderBy(_ => Random.Shared.Next()).ToList()))
                .ToList(),
            MinTopics,
            MaxTopics));

    [HttpPost("placement")]
    public async Task<ActionResult<PlacementResultDto>> Submit(PlacementSubmitRequest request, CancellationToken cancellationToken)
    {
        if (test.Questions.Count == 0)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Вступний тест ще не готовий.");
        }

        if (!test.MatchesQuota(request.Answers.Select(a => a.QuestionId)))
        {
            return ValidationProblem("Відповіді не збігаються з питаннями тесту. Почни тест заново.");
        }

        var user = await db.Users.SingleAsync(u => u.Id == User.UserId(), cancellationToken);
        var answers = request.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().Choice);
        var (level, scores) = test.Evaluate(answers);
        user.Level = level;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new PlacementResultDto(level, scores, UserDto.From(user, await db.IsTeacherAsync(user.Id, cancellationToken))));
    }

    [HttpPut("profile/topics")]
    public async Task<ActionResult<UserDto>> SetTopics(FavoriteTopicsRequest request, CancellationToken cancellationToken)
    {
        var wanted = request.Topics.Select(t => t.Trim().ToLowerInvariant()).Where(t => t.Length > 0).Distinct().ToList();
        var topics = await db.Tags.AsNoTracking()
            .Where(t => t.Kind == TagKind.Topic && t.Status == ModerationStatus.Approved && wanted.Contains(t.Name.ToLower()))
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToListAsync(cancellationToken);
        if (topics.Count != wanted.Count || topics.Count < MinTopics || topics.Count > MaxTopics)
        {
            return ValidationProblem($"Обери від {MinTopics} до {MaxTopics} тем зі списку.");
        }

        var user = await db.Users.SingleAsync(u => u.Id == User.UserId(), cancellationToken);
        user.FavoriteTopics = topics;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(UserDto.From(user, await db.IsTeacherAsync(user.Id, cancellationToken)));
    }
}
