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
[Route("api/teaching")]
public class TeachingController(FabulaDbContext db) : ControllerBase
{
    [Authorize(Policy = Policies.CanAuthor)]
    [HttpPost("invitations")]
    public async Task<ActionResult<TeachingLinkDto>> Invite(InviteStudentRequest request, CancellationToken cancellationToken)
    {
        var identifier = request.EmailOrUsername.Trim().ToLowerInvariant().TrimStart('@');
        var student = await db.Users.SingleOrDefaultAsync(
            u => (u.Email == identifier || u.Username == identifier) && u.Role == UserRole.Student, cancellationToken);
        if (student is null)
        {
            return NotFound(new ProblemDetails { Title = "Учня з такою поштою або ніком не знайдено." });
        }

        var teacherId = User.UserId();
        if (await db.TeachingLinks.AnyAsync(l => l.TeacherId == teacherId && l.StudentId == student.Id, cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "Цей учень уже запрошений або вже є вашим учнем." });
        }

        var link = new TeachingLink
        {
            TeacherId = teacherId,
            StudentId = student.Id,
            Status = TeachingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        db.TeachingLinks.Add(link);
        await db.SaveChangesAsync(cancellationToken);
        return Ok((await LoadLinksAsync(l => l.Id == link.Id, cancellationToken)).Single());
    }

    [HttpGet("students")]
    public async Task<ActionResult<IReadOnlyList<TeachingLinkDto>>> Students(CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        return Ok(await LoadLinksAsync(l => l.TeacherId == userId, cancellationToken));
    }

    [HttpGet("teachers")]
    public async Task<ActionResult<IReadOnlyList<TeachingLinkDto>>> Teachers(CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        return Ok(await LoadLinksAsync(l => l.StudentId == userId, cancellationToken));
    }

    [HttpPost("links/{id:guid}/accept")]
    public async Task<ActionResult<TeachingLinkDto>> Accept(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var link = await db.TeachingLinks.SingleOrDefaultAsync(l => l.Id == id && l.StudentId == userId, cancellationToken);
        if (link is null)
        {
            return NotFound();
        }

        if (link.Status == TeachingStatus.Pending)
        {
            link.Status = TeachingStatus.Accepted;
            link.AcceptedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok((await LoadLinksAsync(l => l.Id == id, cancellationToken)).Single());
    }

    [HttpDelete("links/{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var link = await db.TeachingLinks.SingleOrDefaultAsync(
            l => l.Id == id && (l.StudentId == userId || l.TeacherId == userId), cancellationToken);
        if (link is null)
        {
            return NotFound();
        }

        db.TeachingLinks.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<List<TeachingLinkDto>> LoadLinksAsync(
        System.Linq.Expressions.Expression<Func<TeachingLink, bool>> filter, CancellationToken cancellationToken)
    {
        var links = await db.TeachingLinks.AsNoTracking()
            .Include(l => l.Teacher)
            .Include(l => l.Student)
            .Where(filter)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
        var teachers = await db.TeacherIdsAsync(links.SelectMany(l => new[] { l.TeacherId, l.StudentId }), cancellationToken);
        var viewerId = User.UserId();

        PublicUserDto ToPublic(User user, bool viewerIsAcceptedTeacher) =>
            new(user.Id, user.Username, NamePolicy.VisibleName(user, viewerId, viewerIsAcceptedTeacher), teachers.Contains(user.Id), AvatarDto.From(user));

        return links.Select(l => new TeachingLinkDto(
            l.Id,
            ToPublic(l.Teacher!, viewerIsAcceptedTeacher: false),
            ToPublic(l.Student!, viewerIsAcceptedTeacher: l.TeacherId == viewerId && l.Status == TeachingStatus.Accepted),
            l.Status,
            l.CreatedAt,
            l.AcceptedAt)).ToList();
    }
}
