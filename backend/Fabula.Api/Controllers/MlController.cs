using System.ComponentModel.DataAnnotations;
using Fabula.Api.Auth;
using Fabula.Api.Entities;
using Fabula.Api.Ml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fabula.Api.Controllers;

public record AnalyzeTextsRequest([MaxLength(100)] IReadOnlyList<string> Texts, CefrLevel Level, [MaxLength(100)] IReadOnlyList<bool>? Narration = null);

[ApiController]
[Authorize(Policy = Policies.CanAuthor)]
[EnableRateLimiting(RateLimits.Ml)]
[Route("api/ml")]
public class MlController(MlClient ml) : ControllerBase
{
    private const int MaxTextLength = 500;

    [HttpGet("calibration")]
    public async Task<ActionResult<System.Text.Json.Nodes.JsonNode>> Calibration(CancellationToken cancellationToken)
    {
        var calibration = await ml.CalibrationAsync(cancellationToken);
        return calibration is null
            ? Problem("Оцінка рівня тимчасово недоступна: ML-сервіс не відповідає.", statusCode: StatusCodes.Status503ServiceUnavailable)
            : Ok(calibration);
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<MlAnalysis>> Analyze(AnalyzeTextsRequest request, CancellationToken cancellationToken)
    {
        if (request.Texts.Any(t => t.Length > MaxTextLength))
        {
            return ValidationProblem($"Репліка не може бути довшою за {MaxTextLength} символів.");
        }

        var analysis = await ml.AnalyzeAsync(request.Texts, request.Narration ?? [], request.Level, cancellationToken);
        return analysis is null
            ? Problem("Оцінка рівня тимчасово недоступна: ML-сервіс не відповідає.", statusCode: StatusCodes.Status503ServiceUnavailable)
            : Ok(analysis);
    }
}
