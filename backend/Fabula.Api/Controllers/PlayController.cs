using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class PlayController(FabulaDbContext db) : ControllerBase
{
    [HttpPost("play/{storyId:guid}/start")]
    public async Task<ActionResult<PlaySessionDto>> Start(Guid storyId, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var story = await db.Stories.AsNoTracking().SingleOrDefaultAsync(s => s.Id == storyId, cancellationToken);
        var canPreview = story is not null && (story.AuthorId == userId || User.IsAdmin());
        if (story is null || (story.Status != StoryStatus.Published && !canPreview))
        {
            return NotFound();
        }

        if (story.StartSceneId is null)
        {
            return ValidationProblem("В історії ще немає сцен.");
        }

        var session = new GameSession
        {
            UserId = userId,
            StoryId = story.Id,
            StartedAt = DateTime.UtcNow,
            ScenePath = [story.StartSceneId.Value]
        };
        db.GameSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new PlaySessionDto(session.Id, story.Id, story.StartSceneId));
    }

    [HttpPost("play/sessions/{sessionId:guid}/choice")]
    public async Task<ActionResult<ChoiceResultDto>> Choose(Guid sessionId, MakeChoiceRequest request, CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        if (session.FinishedAt is not null)
        {
            return ValidationProblem("Це проходження вже завершене.");
        }

        var currentSceneId = session.ScenePath.LastOrDefault();
        var choice = await db.Choices.AsNoTracking().SingleOrDefaultAsync(
            c => c.Id == request.ChoiceId && c.SceneId == currentSceneId, cancellationToken);
        if (choice is null)
        {
            return ValidationProblem("Цей вибір недоступний у поточній сцені.");
        }

        session.ChoiceIds = [.. session.ChoiceIds, choice.Id];
        if (choice.NextSceneId is not null)
        {
            session.ScenePath = [.. session.ScenePath, choice.NextSceneId.Value];
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new ChoiceResultDto(choice.NextSceneId, choice.NextSceneId is null));
    }

    [HttpPost("play/sessions/{sessionId:guid}/finish")]
    public async Task<ActionResult<PlaySummaryDto>> Finish(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        session.FinishedAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var words = await db.UserWords.AsNoTracking()
            .Where(w => w.UserId == session.UserId && w.SourceStoryId == session.StoryId && w.LastSeenAt >= session.StartedAt)
            .OrderBy(w => w.SurfaceForm)
            .Select(w => new StoryWordDto(w.SurfaceForm, w.Translation))
            .ToListAsync(cancellationToken);
        return Ok(new PlaySummaryDto(
            session.ScenePath.Distinct().Count(),
            session.ChoiceIds.Count,
            words,
            session.CrystalsEarned,
            Math.Round((session.FinishedAt.Value - session.StartedAt).TotalMinutes, 1)));
    }

    [HttpPost("words")]
    public async Task<ActionResult<UserWordDto>> SaveWord(SaveWordRequest request, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var surface = request.Word.Trim();
        var lemma = surface.ToLowerInvariant();
        var now = DateTime.UtcNow;
        var word = await db.UserWords.SingleOrDefaultAsync(w => w.UserId == userId && w.Lemma == lemma, cancellationToken);
        if (word is null)
        {
            word = new UserWord
            {
                UserId = userId,
                Lemma = lemma,
                SurfaceForm = surface,
                Translation = request.Translation?.Trim() ?? "",
                FirstSeenAt = now,
                SourceStoryId = request.StoryId
            };
            db.UserWords.Add(word);
        }

        word.HistorySeen++;
        word.SessionSeen++;
        word.LastSeenAt = now;
        word.SourceStoryId = request.StoryId ?? word.SourceStoryId;
        if (string.IsNullOrEmpty(word.Translation) && !string.IsNullOrWhiteSpace(request.Translation))
        {
            word.Translation = request.Translation.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new UserWordDto(word.SurfaceForm, word.Translation, word.HistorySeen, word.FirstSeenAt, word.LastSeenAt));
    }

    [HttpGet("words")]
    public async Task<ActionResult<IReadOnlyList<UserWordDto>>> Words(CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var words = await db.UserWords.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.LastSeenAt)
            .Select(w => new UserWordDto(w.SurfaceForm, w.Translation, w.HistorySeen, w.FirstSeenAt, w.LastSeenAt))
            .ToListAsync(cancellationToken);
        return Ok(words);
    }

    private Task<GameSession?> FindSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        return db.GameSessions.SingleOrDefaultAsync(g => g.Id == sessionId && g.UserId == userId, cancellationToken);
    }
}
