using System.Text.RegularExpressions;
using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Fabula.Api.Ml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

[ApiController]
[Route("api/tags")]
public partial class TagsController(FabulaDbContext db, MlClient ml) : ControllerBase
{
    private const int MaxPendingPerAuthor = 20;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> Approved(CancellationToken cancellationToken)
    {
        var tags = await db.Tags.AsNoTracking()
            .Where(t => t.Status == ModerationStatus.Approved)
            .OrderBy(t => t.Kind).ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
        return Ok(tags.Select(t => t.ToDto()).ToList());
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> Available(CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var tags = await db.Tags.AsNoTracking()
            .Where(t => t.Status == ModerationStatus.Approved || (t.Status == ModerationStatus.Pending && t.CreatedById == userId))
            .OrderBy(t => t.Kind).ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
        return Ok(tags.Select(t => t.ToDto()).ToList());
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpPost]
    public async Task<ActionResult<TagDto>> Create(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var name = Spaces().Replace(request.Name.Trim(), " ");
        if (!AllowedName().IsMatch(name))
        {
            return ValidationProblem("Назва тегу: 2–30 символів, починається з літери; можна цифри, пробіли, дефіс і апостроф.");
        }

        name = char.ToUpper(name[0]) + name[1..];
        var lowered = name.ToLower();
        var existing = await db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == lowered, cancellationToken);
        if (existing is not null)
        {
            return existing.Status == ModerationStatus.Rejected
                ? ValidationProblem("Цей тег не пройшов модерацію і не може бути використаний.")
                : Ok(existing.ToDto());
        }

        var userId = User.UserId();
        var pending = await db.Tags.CountAsync(t => t.CreatedById == userId && t.Status == ModerationStatus.Pending, cancellationToken);
        if (pending >= MaxPendingPerAuthor)
        {
            return ValidationProblem("Забагато тегів чекають на перевірку. Дочекайся рішення модератора.");
        }

        var check = (await ml.ModerateTextsAsync([name], cancellationToken))?.SingleOrDefault();
        if (check?.Verdict == "blocked")
        {
            return ValidationProblem("Такий тег не можна створити: він не пройшов автоматичну перевірку.");
        }

        var approved = check is { Verdict: "ok", Age: "6+" };
        var tag = new Tag { Name = name, Kind = request.Kind, Status = approved ? ModerationStatus.Approved : ModerationStatus.Pending, CreatedById = userId };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(cancellationToken);
        if (!approved)
        {
            db.ModerationItems.Add(new ModerationItem
            {
                TargetType = ModerationTarget.Tag,
                TargetId = Guid.Empty,
                TagId = tag.Id,
                Reason = check is null ? "Автоматична перевірка недоступна" : $"Тег «{name}»: {string.Join(", ", check.Reasons.DefaultIfEmpty("груба лексика"))}",
                Status = ModerationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(tag.ToDto());
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"^[\p{L}][\p{L}\p{N} '’-]{1,29}$")]
    private static partial Regex AllowedName();
}
