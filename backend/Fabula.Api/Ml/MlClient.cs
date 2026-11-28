using System.Net.Http.Json;
using Fabula.Api.Entities;

namespace Fabula.Api.Ml;

public record MlWord(string Text, int Start, int End, string Lemma, string? Level, bool AboveTarget, bool Rare, IReadOnlyList<string> Simpler);

public record MlSentence(int Start, int End, string Level, double Score);

public record MlFlaggedWord(string Text, int Start, int End, string Kind);

public record MlTextModeration(string Age, string Verdict, IReadOnlyList<string> Reasons, IReadOnlyList<MlFlaggedWord> Words);

public record MlTextModerations(IReadOnlyList<MlTextModeration> Results);

public record MlImageModeration(string Age, string Verdict, IReadOnlyList<string> Reasons);

public record MlLine(
    MlTextModeration? Moderation,
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

    public async Task<string?> ModelNameAsync(CancellationToken cancellationToken)
    {
        try
        {
            var health = await http.GetFromJsonAsync<System.Text.Json.Nodes.JsonNode>("/health", cancellationToken);
            return health?["model"]?.GetValue<string>();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<MlTextModeration>?> ModerateTextsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        try
        {
            var results = new List<MlTextModeration>();
            foreach (var chunk in texts.Chunk(200))
            {
                var response = await http.PostAsJsonAsync("/moderate/texts", new { texts = chunk }, cancellationToken);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadFromJsonAsync<MlTextModerations>(cancellationToken);
                results.AddRange(body?.Results ?? []);
            }

            return results;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning("ML-сервіс недоступний: {Message}", exception.Message);
            return null;
        }
    }

    public async Task<MlImageModeration?> ModerateImageAsync(byte[] data, CancellationToken cancellationToken)
    {
        try
        {
            using var content = new ByteArrayContent(data);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            var response = await http.PostAsync("/moderate/image", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MlImageModeration>(cancellationToken);
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
