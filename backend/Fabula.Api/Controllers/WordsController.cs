using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Dictionary;
using Fabula.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/words")]
public class WordsController(FabulaDbContext db, WordDictionary dictionary) : ControllerBase
{
    private const int CorrectInRowToLearn = 3;
    private const int MaxQuizSize = 20;
    private const int OptionCount = 4;

    [HttpPost]
    public async Task<ActionResult<UserWordDto>> Save(SaveWordRequest request, CancellationToken cancellationToken)
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

        word.TimesTapped++;
        word.LastSeenAt = now;
        word.SourceStoryId ??= request.StoryId;
        if (string.IsNullOrEmpty(word.Translation) && !string.IsNullOrWhiteSpace(request.Translation))
        {
            word.Translation = request.Translation.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok((await LoadAsync(w => w.Id == word.Id, cancellationToken)).Single());
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserWordDto>>> List(CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        return Ok(await LoadAsync(w => w.UserId == userId, cancellationToken));
    }

    [HttpPut("{id:guid}/known")]
    public async Task<ActionResult<UserWordDto>> SetKnown(Guid id, WordKnownRequest request, CancellationToken cancellationToken)
    {
        var word = await FindAsync(id, cancellationToken);
        if (word is null)
        {
            return NotFound();
        }

        word.MarkedKnown = request.Known;
        word.LearnedAt = request.Known ? DateTime.UtcNow : null;
        if (!request.Known)
        {
            word.CorrectInRow = 0;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok((await LoadAsync(w => w.Id == id, cancellationToken)).Single());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var deleted = await db.UserWords.Where(w => w.Id == id && w.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? NotFound() : NoContent();
    }

    [HttpGet("quiz")]
    public async Task<ActionResult<IReadOnlyList<QuizQuestionDto>>> Quiz([FromQuery] int count, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var size = Math.Clamp(count == 0 ? 10 : count, 1, MaxQuizSize);
        var words = await db.UserWords.AsNoTracking()
            .Where(w => w.UserId == userId && w.Translation != "")
            .ToListAsync(cancellationToken);
        var random = Random.Shared;
        var picked = words
            .Where(w => w.LearnedAt == null)
            .OrderBy(w => w.LastPracticedAt ?? DateTime.MinValue)
            .ThenBy(_ => random.Next())
            .Take(size)
            .OrderBy(_ => random.Next())
            .ToList();

        var questions = picked.Select(word =>
        {
            var direction = random.Next(2) == 0 ? QuizDirection.EnglishToUkrainian : QuizDirection.UkrainianToEnglish;
            var toUkrainian = direction == QuizDirection.EnglishToUkrainian;
            var correct = toUkrainian ? word.Translation : word.SurfaceForm;
            var pool = words.Where(w => w.Id != word.Id).Select(w => toUkrainian ? w.Translation : w.SurfaceForm).ToList();
            var options = new List<string> { correct };
            foreach (var candidate in pool.OrderBy(_ => random.Next()))
            {
                if (options.Count == OptionCount)
                {
                    break;
                }

                if (!options.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    options.Add(candidate);
                }
            }

            for (var attempt = 0; options.Count < OptionCount && attempt < 50; attempt++)
            {
                var entry = dictionary.RandomEntry(random);
                if (entry is null)
                {
                    break;
                }

                var candidate = toUkrainian ? entry.Value.Translation : entry.Value.Word;
                if (!options.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    options.Add(candidate);
                }
            }

            return new QuizQuestionDto(word.Id, direction, toUkrainian ? word.SurfaceForm : word.Translation, options.OrderBy(_ => random.Next()).ToList());
        }).ToList();
        return Ok(questions);
    }

    [HttpPost("{id:guid}/answer")]
    public async Task<ActionResult<QuizAnswerDto>> Answer(Guid id, QuizAnswerRequest request, CancellationToken cancellationToken)
    {
        var word = await FindAsync(id, cancellationToken);
        if (word is null)
        {
            return NotFound();
        }

        var expected = request.Direction == QuizDirection.EnglishToUkrainian ? word.Translation : word.SurfaceForm;
        var correct = string.Equals(request.Answer.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        word.HistorySeen++;
        word.SessionSeen = 1;
        word.SessionCorrect = correct ? 1 : 0;
        word.LastPracticedAt = now;
        if (correct)
        {
            word.HistoryCorrect++;
            word.CorrectInRow++;
            if (word.CorrectInRow >= CorrectInRowToLearn)
            {
                word.LearnedAt ??= now;
            }
        }
        else
        {
            word.CorrectInRow = 0;
            word.LearnedAt = null;
            word.MarkedKnown = false;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new QuizAnswerDto(correct, expected, word.CorrectInRow, word.LearnedAt is not null));
    }

    private Task<UserWord?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        return db.UserWords.SingleOrDefaultAsync(w => w.Id == id && w.UserId == userId, cancellationToken);
    }

    private async Task<List<UserWordDto>> LoadAsync(
        System.Linq.Expressions.Expression<Func<UserWord, bool>> filter, CancellationToken cancellationToken) =>
        await db.UserWords.AsNoTracking()
            .Where(filter)
            .OrderByDescending(w => w.LastSeenAt)
            .Select(w => new UserWordDto(
                w.Id,
                w.SurfaceForm,
                w.Translation,
                db.Stories.Where(s => s.Id == w.SourceStoryId).Select(s => s.Title).FirstOrDefault(),
                w.TimesTapped,
                w.HistorySeen,
                w.HistoryCorrect,
                w.CorrectInRow,
                w.LearnedAt != null,
                w.MarkedKnown,
                w.FirstSeenAt,
                w.LastSeenAt))
            .ToListAsync(cancellationToken);
}
