using System.Net.Http.Json;
using Fabula.Api.Entities;

namespace Fabula.Api.Ml;

public record MlWord(string Text, int Start, int End, string Lemma, string? Level, bool AboveTarget, bool Rare, IReadOnlyList<string> Simpler);

public record MlSentence(int Start, int End, string Level, double Score);

public record MlLine(
    string? Level,
    double? Score,
    double? Confidence,
    string Scale,
    bool TooShort,
    IReadOnlyList<MlSentence> Sentences,
    IReadOnlyList<MlWord> Words);

public record MlAnalysis(string Model, IReadOnlyList<MlLine> Results);

public class MlClient(HttpClient http, ILogger<MlClient> logger)
{
    public async Task<MlAnalysis?> AnalyzeAsync(
        IReadOnlyList<string> texts, IReadOnlyList<bool> narration, CefrLevel targetLevel, CancellationToken cancellationToken)
    {
        if (texts.Count == 0)
        {
            return new MlAnalysis("", []);
        }

        try
        {
            var response = await http.PostAsJsonAsync(
                "/analyze", new { texts, narration, targetLevel = targetLevel.ToString() }, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MlAnalysis>(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning("ML-сервіс недоступний: {Message}", exception.Message);
            return null;
        }
    }

    public async Task<System.Text.Json.Nodes.JsonNode?> CalibrationAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await http.GetFromJsonAsync<System.Text.Json.Nodes.JsonNode>("/calibration", cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning("ML-сервіс недоступний: {Message}", exception.Message);
            return null;
        }
    }
}
