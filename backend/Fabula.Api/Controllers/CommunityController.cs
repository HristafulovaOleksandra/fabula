using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Entities;
using Fabula.Api.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

[ApiController]
[Route("api")]
public class CommunityController(FabulaDbContext db, ImageStorage storage) : ControllerBase
{
    [Authorize]
    [HttpPatch("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.UserId()], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (request.DisplayName is not null)
        {
            user.DisplayName = request.DisplayName.Trim();
        }

        if (request.NameVisibility is not null)
        {
            user.NameVisibility = request.NameVisibility.Value;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(UserDto.From(user, await db.IsTeacherAsync(user.Id, cancellationToken)));
    }

    [Authorize]
    [HttpDelete("profile")]
    public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([User.UserId()], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ValidationProblem("Неправильний пароль. Акаунт не видалено.");
        }

        var imagePaths = await db.Backgrounds.Where(b => b.OwnerId == user.Id).Select(b => b.ImagePath)
            .Concat(db.Stories.Where(s => s.AuthorId == user.Id && s.CoverImagePath != null).Select(s => s.CoverImagePath!))
            .Concat(db.Characters.Where(c => c.ImagePath != null && db.Scenes.Any(s => s.Id == c.SceneId && s.Story!.AuthorId == user.Id)).Select(c => c.ImagePath!))
            .ToListAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.GameSessions.Where(g => g.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        await db.Transactions.Where(t => t.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        await db.Backgrounds.Where(b => b.OwnerId == user.Id).ExecuteDeleteAsync(cancellationToken);
        await db.ModerationItems.Where(m => m.ReviewedById == user.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(m => m.ReviewedById, (Guid?)null), cancellationToken);
        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        foreach (var path in imagePaths.Distinct())
        {
            storage.DeleteIfOwned(path);
        }

        return NoContent();
    }

    [HttpGet("authors/{username}")]
    public async Task<ActionResult<AuthorProfileDto>> Author(string username, CancellationToken cancellationToken)
    {
        var author = await FindAuthorAsync(username, cancellationToken);
        if (author is null)
        {
            return NotFound();
        }

        Guid? viewerId = User.Identity?.IsAuthenticated == true ? User.UserId() : null;
        var stories = await db.Stories.AsNoTracking()
            .Include(s => s.Author).Include(s => s.Tags).Include(s => s.Scenes)
            .Where(s => s.AuthorId == author.Id && s.Status == StoryStatus.Published)
            .OrderByDescending(s => s.UpdatedAt)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        var followers = await db.Follows.CountAsync(f => f.AuthorId == author.Id, cancellationToken);
        var followed = viewerId is not null && await db.Follows.AnyAsync(f => f.AuthorId == author.Id && f.FollowerId == viewerId, cancellationToken);
        var profile = new PublicUserDto(
            author.Id, author.Username, NamePolicy.VisibleName(author, viewerId, false), await db.IsTeacherAsync(author.Id, cancellationToken));
        return Ok(new AuthorProfileDto(profile, followers, followed, stories.Select(s => s.ToSummary()).ToList()));
    }

    [Authorize]
    [HttpPost("authors/{username}/follow")]
    public async Task<IActionResult> Follow(string username, CancellationToken cancellationToken)
    {
        var author = await FindAuthorAsync(username, cancellationToken);
        var followerId = User.UserId();
        if (author is null || author.Id == followerId)
        {
            return NotFound();
        }

        if (!await db.Follows.AnyAsync(f => f.AuthorId == author.Id && f.FollowerId == followerId, cancellationToken))
        {
            db.Follows.Add(new Follow { AuthorId = author.Id, FollowerId = followerId, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [Authorize]
    [HttpDelete("authors/{username}/follow")]
    public async Task<IActionResult> Unfollow(string username, CancellationToken cancellationToken)
    {
        var author = await FindAuthorAsync(username, cancellationToken);
        if (author is null)
        {
            return NotFound();
        }

        var followerId = User.UserId();
        await db.Follows.Where(f => f.AuthorId == author.Id && f.FollowerId == followerId).ExecuteDeleteAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("follows")]
    public async Task<ActionResult<IReadOnlyList<PublicUserDto>>> Follows(CancellationToken cancellationToken)
    {
        var followerId = User.UserId();
        var authors = await db.Follows.AsNoTracking()
            .Where(f => f.FollowerId == followerId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Author!)
            .ToListAsync(cancellationToken);
        var teachers = await db.TeacherIdsAsync(authors.Select(a => a.Id), cancellationToken);
        return Ok(authors.Select(a => new PublicUserDto(a.Id, a.Username, NamePolicy.PublicName(a), teachers.Contains(a.Id))).ToList());
    }

    [Authorize]
    [HttpGet("stories/following")]
    public async Task<ActionResult<IReadOnlyList<StorySummaryDto>>> FollowingFeed(CancellationToken cancellationToken)
    {
        var followerId = User.UserId();
        var authorIds = db.Follows.Where(f => f.FollowerId == followerId).Select(f => f.AuthorId);
        var stories = await db.Stories.AsNoTracking()
            .Include(s => s.Author).Include(s => s.Tags).Include(s => s.Scenes)
            .Where(s => s.Status == StoryStatus.Published && authorIds.Contains(s.AuthorId))
            .OrderByDescending(s => s.UpdatedAt)
            .Take(50)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        return Ok(stories.Select(s => s.ToSummary()).ToList());
    }

    private Task<User?> FindAuthorAsync(string username, CancellationToken cancellationToken)
    {
        var normalized = username.Trim().TrimStart('@').ToLowerInvariant();
        return db.Users.AsNoTracking().SingleOrDefaultAsync(
            u => u.Username == normalized && (u.Role == UserRole.Author || u.Role == UserRole.Admin), cancellationToken);
    }
}
