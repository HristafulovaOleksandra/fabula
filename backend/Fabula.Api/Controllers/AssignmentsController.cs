using System.ComponentModel.DataAnnotations;
using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Economy;
using Fabula.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

public record CreateAssignmentRequest(Guid StudentId, Guid StoryId, DateOnly? DueDate, [MaxLength(300)] string? Note);

public enum AssignmentState
{
    Open,
    Overdue,
    Done
}

public record AssignmentDto(
    Guid Id,
    PublicUserDto Teacher,
    PublicUserDto Student,
    StorySummaryDto Story,
    DateOnly? DueDate,
    string Note,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    AssignmentState State);

[ApiController]
[Authorize]
[Route("api/assignments")]
public class AssignmentsController(FabulaDbContext db, WalletService wallet) : ControllerBase
{
    [Authorize(Policy = Policies.CanAuthor)]
    [HttpPost]
    public async Task<ActionResult<AssignmentDto>> Create(CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var teacherId = User.UserId();
        var linked = await db.TeachingLinks.AnyAsync(
            l => l.TeacherId == teacherId && l.StudentId == request.StudentId && l.Status == TeachingStatus.Accepted, cancellationToken);
        if (!linked)
        {
            return ValidationProblem("Призначати історії можна лише своїм учням, які прийняли запрошення.");
        }

        var published = await db.Stories.AnyAsync(s => s.Id == request.StoryId && s.Status == StoryStatus.Published, cancellationToken);
        if (!published)
        {
            return ValidationProblem("Призначити можна лише опубліковану історію.");
        }

        if (request.DueDate is not null && request.DueDate < wallet.Today())
        {
            return ValidationProblem("Термін не може бути в минулому.");
        }

        var assignment = await db.Assignments.SingleOrDefaultAsync(
            a => a.StudentId == request.StudentId && a.StoryId == request.StoryId, cancellationToken);
        if (assignment is not null && assignment.CompletedAt is null)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: "Цю історію вже призначено цьому учневі.");
        }

        if (assignment is null)
        {
            assignment = new Assignment { StudentId = request.StudentId, StoryId = request.StoryId };
            db.Assignments.Add(assignment);
        }

        assignment.TeacherId = teacherId;
        assignment.DueDate = request.DueDate;
        assignment.Note = request.Note?.Trim() ?? "";
        assignment.CreatedAt = DateTime.UtcNow;
        assignment.CompletedAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return Ok((await LoadAsync(a => a.Id == assignment.Id, cancellationToken)).Single());
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpGet("given")]
    public async Task<ActionResult<IReadOnlyList<AssignmentDto>>> Given(CancellationToken cancellationToken)
    {
        var teacherId = User.UserId();
        return Ok(await LoadAsync(a => a.TeacherId == teacherId, cancellationToken));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<AssignmentDto>>> Mine(CancellationToken cancellationToken)
    {
        var studentId = User.UserId();
        var linkedTeachers = db.TeachingLinks
            .Where(l => l.StudentId == studentId && l.Status == TeachingStatus.Accepted)
            .Select(l => l.TeacherId);
        return Ok(await LoadAsync(a => a.StudentId == studentId && linkedTeachers.Contains(a.TeacherId), cancellationToken));
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var teacherId = User.UserId();
        var deleted = await db.Assignments.Where(a => a.Id == id && a.TeacherId == teacherId).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? NotFound() : NoContent();
    }

    private async Task<List<AssignmentDto>> LoadAsync(
        System.Linq.Expressions.Expression<Func<Assignment, bool>> filter, CancellationToken cancellationToken)
    {
        var assignments = await db.Assignments.AsNoTracking().Where(filter).ToListAsync(cancellationToken);
        var storyIds = assignments.Select(a => a.StoryId).Distinct().ToList();
        var userIds = assignments.SelectMany(a => new[] { a.TeacherId, a.StudentId }).Distinct().ToList();
        var stories = await db.Stories.AsNoTracking()
            .Include(s => s.Author).Include(s => s.Tags).Include(s => s.Scenes)
            .Where(s => storyIds.Contains(s.Id))
            .AsSplitQuery()
            .ToDictionaryAsync(s => s.Id, cancellationToken);
        var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);
        var teachers = await db.TeacherIdsAsync(userIds, cancellationToken);
        var viewerId = User.UserId();
        var today = wallet.Today();

        PublicUserDto ToPublic(User user, bool viewerIsTeacher) =>
            new(user.Id, user.Username, NamePolicy.VisibleName(user, viewerId, viewerIsTeacher), teachers.Contains(user.Id), AvatarDto.From(user));

        return assignments
            .Where(a => stories.ContainsKey(a.StoryId))
            .Select(a => new AssignmentDto(
                a.Id,
                ToPublic(users[a.TeacherId], false),
                ToPublic(users[a.StudentId], a.TeacherId == viewerId),
                stories[a.StoryId].ToSummary(),
                a.DueDate,
                a.Note,
                a.CreatedAt,
                a.CompletedAt,
                a.CompletedAt is not null ? AssignmentState.Done
                    : a.DueDate is not null && a.DueDate < today ? AssignmentState.Overdue
                    : AssignmentState.Open))
            .OrderBy(a => a.State == AssignmentState.Done)
            .ThenBy(a => a.DueDate ?? DateOnly.MaxValue)
            .ThenByDescending(a => a.CreatedAt)
            .ToList();
    }
}
