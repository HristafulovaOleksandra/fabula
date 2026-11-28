namespace Fabula.Api.Dictionary;

public class WordDictionary
{
    private readonly Dictionary<string, string[]> entries = new(StringComparer.Ordinal);
    private string[] words = [];

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

        words = entries.Keys.Where(key => key.Length >= 4 && key.All(char.IsLetter)).ToArray();
    }

    public IReadOnlyList<string> Translate(string word) => Lookup(word).Translations;

    public (string Lemma, IReadOnlyList<string> Translations) Lookup(string word)
    {
        var normalized = word.Trim().ToLowerInvariant();
        var found = Candidates(normalized)
            .Where(entries.ContainsKey)
            .Select(candidate => (Lemma: candidate, Translations: entries[candidate]))
            .ToList();
        if (found.Count == 0)
        {
            return (normalized, []);
        }

        var exact = found[0];
        var baseForm = found.Skip(1).FirstOrDefault(candidate => candidate.Translations.Intersect(exact.Translations).Any());
        return baseForm.Lemma is null ? exact : baseForm;
    }

    public (string Word, string Translation)? RandomEntry(Random random)
    {
        if (words.Length == 0)
        {
            return null;
        }

        var word = words[random.Next(words.Length)];
        return (word, entries[word][0]);
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
