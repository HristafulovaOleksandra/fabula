using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Data;
using Fabula.Api.Economy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wallet")]
public class WalletController(FabulaDbContext db, WalletService wallet, EconomySettings economy) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WalletDto>> Get(CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == User.UserId(), cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var streak = await wallet.LoadStreakAsync(user.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await wallet.DescribeAsync(user, streak, cancellationToken));
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> Transactions(CancellationToken cancellationToken)
    {
        var userId = User.UserId();
        var items = await db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(30)
            .Select(t => new TransactionDto(
                t.Id,
                t.Amount,
                t.Reason,
                db.Stories.Where(s => s.Id == t.StoryId).Select(s => s.Title).FirstOrDefault(),
                t.CreatedAt))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("streak/rescue")]
    public async Task<ActionResult<RescueResultDto>> Rescue(CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == User.UserId(), cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var streak = await wallet.LoadStreakAsync(user.Id, cancellationToken);
        if (!wallet.CanRescue(streak))
        {
            await db.SaveChangesAsync(cancellationToken);
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: "Немає серії, яку можна відновити.");
        }

        if (user.CrystalBalance < economy.StreakFreezePrice)
        {
            await db.SaveChangesAsync(cancellationToken);
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: WalletService.NotEnough(economy.StreakFreezePrice, user.CrystalBalance));
        }

        var rewards = wallet.Rescue(user, streak);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new RescueResultDto(await wallet.DescribeAsync(user, streak, cancellationToken), rewards));
    }
}
