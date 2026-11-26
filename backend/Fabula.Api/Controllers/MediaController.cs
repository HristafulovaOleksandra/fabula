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
public class MediaController(FabulaDbContext db, ImageStorage storage) : ControllerBase
{
    private static readonly string[] UploadFolders = ["covers", "characters"];

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpGet("backgrounds")]
    public async Task<ActionResult<IReadOnlyList<BackgroundDto>>> Backgrounds(CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var backgrounds = await db.Backgrounds.AsNoTracking()
            .Where(b => b.OwnerId == null || b.OwnerId == userId)
            .OrderBy(b => b.OwnerId != null).ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);
        return Ok(backgrounds.Select(b => b.ToDto()).ToList());
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpPost("backgrounds")]
    [RequestSizeLimit(ImageStorage.MaxBytes + 64 * 1024)]
    public async Task<ActionResult<BackgroundDto>> UploadBackground(
        [FromForm] string name, IFormFile file, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 80)
        {
            return ValidationProblem("Назва фону має містити від 1 до 80 символів.");
        }

        try
        {
            var background = new Background
            {
                Name = name.Trim(),
                ImagePath = await storage.SaveAsync(file, "backgrounds", cancellationToken),
                OwnerId = User.IsAdmin() ? null : User.UserId(),
                CreatedAt = DateTime.UtcNow
            };
            db.Backgrounds.Add(background);
            await db.SaveChangesAsync(cancellationToken);
            return Ok(background.ToDto());
        }
        catch (ImageRejectedException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [Authorize(Policy = Policies.CanAuthor)]
    [HttpPost("uploads/{folder}")]
    [RequestSizeLimit(ImageStorage.MaxBytes + 64 * 1024)]
    public async Task<ActionResult<UploadResponse>> Upload(string folder, IFormFile file, CancellationToken cancellationToken)
    {
        if (!UploadFolders.Contains(folder))
        {
            return NotFound();
        }

        try
        {
            return Ok(new UploadResponse(await storage.SaveAsync(file, folder, cancellationToken)));
        }
        catch (ImageRejectedException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }
}
