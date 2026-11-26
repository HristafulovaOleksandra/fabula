namespace Fabula.Api.Dictionary;

public class WordDictionary
{
    private readonly Dictionary<string, string[]> entries = new(StringComparer.Ordinal);

    public WordDictionary(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split('\t');
            if (parts.Length == 2)
            {
                entries[parts[0]] = parts[1].Split('|', StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }

    public IReadOnlyList<string> Translate(string word)
    {
        var normalized = word.Trim().ToLowerInvariant();
        foreach (var candidate in Candidates(normalized))
        {
            if (entries.TryGetValue(candidate, out var translations))
            {
                return translations;
            }
        }

        return [];
    }

    private static IEnumerable<string> Candidates(string word)
    {
        yield return word;
        if (word.EndsWith("'s"))
        {
            yield return word[..^2];
        }

        if (word.EndsWith("ies") && word.Length > 4)
        {
            yield return word[..^3] + "y";
        }

        if (word.EndsWith("es") && word.Length > 3)
        {
            yield return word[..^2];
        }

        if (word.EndsWith('s') && word.Length > 3)
        {
            yield return word[..^1];
        }

        if (word.EndsWith("ed") && word.Length > 4)
        {
            yield return word[..^2];
            yield return word[..^1];
        }

        if (word.EndsWith("ing") && word.Length > 5)
        {
            yield return word[..^3];
            yield return word[..^3] + "e";
        }
    }
}
