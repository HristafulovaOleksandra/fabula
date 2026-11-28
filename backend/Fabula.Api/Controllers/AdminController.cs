using System.ComponentModel.DataAnnotations;
using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Fabula.Api.Moderation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

public record QueueStoryDto(Guid Id, string Title, string AuthorUsername, StoryStatus Status, AgeRating AgeRating, StoryVisibility Visibility);

public record QueueTagDto(int Id, string Name, TagKind Kind, ModerationStatus Status, string? CreatorUsername);

public record QueueItemDto(
    Guid Id,
    ModerationTarget TargetType,
    ModerationStatus Status,
    string Reason,
    string AuthorMessage,
    string ResolutionNote,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? ReviewedBy,
    QueueStoryDto? Story,
    QueueTagDto? Tag);

public record QueueDecisionRequest([MaxLength(1000)] string? Note);

public record AdminUserDto(
    Guid Id,
    string Username,
    string Email,
    UserRole Role,
    bool IsBlocked,
    bool IsTeacher,
    int StoryCount,
    DateTime CreatedAt,
    AvatarDto Avatar);

public record UserRoleRequest(UserRole Role);

public record UserBlockRequest(bool Blocked);

[ApiController]
[Authorize(Policy = Policies.AdminOnly)]
[Route("api/admin")]
public class AdminController(FabulaDbContext db, StoryModeration moderation) : ControllerBase
{
    [HttpGet("queue")]
    public async Task<ActionResult<IReadOnlyList<QueueItemDto>>> Queue([FromQuery] bool history, CancellationToken cancellationToken)
    {
        var query = db.ModerationItems.AsNoTracking()
            .Where(m => m.TargetType == ModerationTarget.Story || m.TargetType == ModerationTarget.Tag);
        query = history
            ? query.Where(m => m.Status == ModerationStatus.Approved || m.Status == ModerationStatus.Rejected)
                .OrderByDescending(m => m.ReviewedAt).Take(50)
            : query.Where(m => m.Status == ModerationStatus.Pending || m.Status == ModerationStatus.Appealed)
                .OrderBy(m => m.CreatedAt);
        var items = await query.ToListAsync(cancellationToken);

        var storyIds = items.Where(m => m.TargetType == ModerationTarget.Story).Select(m => m.TargetId).ToList();
        var stories = await db.Stories.AsNoTracking().Include(s => s.Author)
            .Where(s => storyIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);
        var tagIds = items.Where(m => m.TagId != null).Select(m => m.TagId!.Value).ToList();
        var tags = await db.Tags.AsNoTracking().Where(t => tagIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);
        var userIds = tags.Values.Select(t => t.CreatedById).OfType<Guid>()
            .Concat(items.Select(m => m.ReviewedById).OfType<Guid>())
            .Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        return Ok(items
            .Select(m =>
            {
                var story = m.TargetType == ModerationTarget.Story && stories.TryGetValue(m.TargetId, out var s)
                    ? new QueueStoryDto(s.Id, s.Title, s.Author?.Username ?? "", s.Status, s.AgeRating, s.Visibility)
                    : null;
                var tag = m.TagId is { } tagId && tags.TryGetValue(tagId, out var t)
                    ? new QueueTagDto(t.Id, t.Name, t.Kind, t.Status, t.CreatedById is { } creator ? names.GetValueOrDefault(creator) : null)
                    : null;
                return new QueueItemDto(
                    m.Id, m.TargetType, m.Status, m.Reason, m.AuthorMessage, m.ResolutionNote, m.CreatedAt, m.ReviewedAt,
                    m.ReviewedById is { } reviewer ? names.GetValueOrDefault(reviewer) : null, story, tag);
            })
            .Where(m => m.Story is not null || m.Tag is not null)
            .ToList());
    }

    [HttpPost("queue/{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, QueueDecisionRequest request, CancellationToken cancellationToken) =>
        DecideAsync(id, approve: true, request.Note, cancellationToken);

    [HttpPost("queue/{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, QueueDecisionRequest request, CancellationToken cancellationToken) =>
        DecideAsync(id, approve: false, request.Note, cancellationToken);

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> Users([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();
        var search = q?.Trim().TrimStart('@').ToLowerInvariant();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = LikePattern.Contains(search);
            query = query.Where(u => EF.Functions.ILike(u.Username, pattern, LikePattern.Escape) || EF.Functions.ILike(u.Email, pattern, LikePattern.Escape));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).Take(50).ToListAsync(cancellationToken);
        var ids = users.Select(u => u.Id).ToList();
        var storyCounts = await db.Stories.Where(s => ids.Contains(s.AuthorId)).GroupBy(s => s.AuthorId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
        var teachers = await db.TeacherIdsAsync(ids, cancellationToken);
        return Ok(users.Select(u => new AdminUserDto(
            u.Id, u.Username, u.Email, u.Role, u.IsBlocked, teachers.Contains(u.Id), storyCounts.GetValueOrDefault(u.Id), u.CreatedAt, AvatarDto.From(u))).ToList());
    }

    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> SetRole(Guid id, UserRoleRequest request, CancellationToken cancellationToken)
    {
        if (id == User.UserId())
        {
            return ValidationProblem("Свою роль змінити не можна.");
        }

        var updated = await db.Users.Where(u => u.Id == id)
            .ExecuteUpdateAsync(update => update.SetProperty(u => u.Role, request.Role), cancellationToken);
        return updated == 0 ? NotFound() : NoContent();
    }

    [HttpPut("users/{id:guid}/blocked")]
    public async Task<IActionResult> SetBlocked(Guid id, UserBlockRequest request, CancellationToken cancellationToken)
    {
        if (id == User.UserId())
        {
            return ValidationProblem("Себе заблокувати не можна.");
        }

        var updated = await db.Users.Where(u => u.Id == id)
            .ExecuteUpdateAsync(update => update.SetProperty(u => u.IsBlocked, request.Blocked), cancellationToken);
        return updated == 0 ? NotFound() : NoContent();
    }

    private async Task<IActionResult> DecideAsync(Guid id, bool approve, string? note, CancellationToken cancellationToken)
    {
        var item = await db.ModerationItems.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (item.Status is not (ModerationStatus.Pending or ModerationStatus.Appealed))
        {
            return ValidationProblem("Це рішення вже ухвалено.");
        }

        if (item.TargetType == ModerationTarget.Story)
        {
            var problem = await DecideStoryAsync(item.TargetId, approve, note, cancellationToken);
            if (problem is not null)
            {
                return problem;
            }
        }
        else if (item.TagId is { } tagId)
        {
            await db.Tags.Where(t => t.Id == tagId).ExecuteUpdateAsync(
                update => update.SetProperty(t => t.Status, approve ? ModerationStatus.Approved : ModerationStatus.Rejected), cancellationToken);
        }

        item.Status = approve ? ModerationStatus.Approved : ModerationStatus.Rejected;
        item.ResolutionNote = note?.Trim() ?? "";
        item.ReviewedById = User.UserId();
        item.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult?> DecideStoryAsync(Guid storyId, bool approve, string? note, CancellationToken cancellationToken)
    {
        var story = await db.Stories.SingleOrDefaultAsync(s => s.Id == storyId, cancellationToken);
        if (story is null)
        {
            return NotFound();
        }

        if (!approve)
        {
            story.Status = StoryStatus.Blocked;
            story.ModerationNote = string.IsNullOrWhiteSpace(note) ? story.ModerationNote : $"Рішення модератора: {note.Trim()}";
            return null;
        }

        var full = await StoryRepository.LoadFullAsync(db, storyId, cancellationToken);
        var report = await moderation.CheckAsync(full!, cancellationToken);
        if (!report.Available)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "ML-сервіс не відповідає, спробуй пізніше.");
        }

        story.ApprovedContent = story.ApprovedContent
            .Concat(report.Findings.Where(f => f.Verdict != ModerationVerdict.Ok).Select(f => f.Text ?? f.ImagePath).OfType<string>())
            .Distinct()
            .ToList();
        story.AgeRating = report.Age;
        story.Status = StoryStatus.Published;
        story.ModerationNote = "";
        return null;
    }
}
