using System.Text;
using Fabula.Api.Entities;

namespace Fabula.Api.Placement;

public record PlacementQuestion(int Id, CefrLevel Level, string Instruction, string Text, IReadOnlyList<string> Options, int Correct);

public record LevelScore(CefrLevel Level, int Correct, int Total);

public class PlacementTest
{
    private const double PassShare = 0.5;

    private static readonly Dictionary<CefrLevel, int> Quota = new()
    {
        [CefrLevel.A1] = 1,
        [CefrLevel.A2] = 2,
        [CefrLevel.B1] = 2,
        [CefrLevel.B2] = 2,
        [CefrLevel.C] = 1,
    };

    private static readonly string[] OptionColumns = ["option1", "option2", "option3", "option4"];

    public IReadOnlyList<PlacementQuestion> Questions { get; }
    public IReadOnlyList<string> Problems { get; }

    private PlacementTest(IReadOnlyList<PlacementQuestion> questions, IReadOnlyList<string> problems)
    {
        Questions = questions;
        Problems = problems;
    }

    public static PlacementTest Load(string path)
    {
        if (!File.Exists(path))
        {
            return new PlacementTest([], [$"Файл з питаннями не знайдено: {path}"]);
        }

        var lines = File.ReadAllLines(path, Encoding.UTF8)
            .Select(line => line.TrimStart('﻿'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
        if (lines.Count == 0)
        {
            return new PlacementTest([], ["Файл з питаннями порожній."]);
        }

        var delimiter = lines[0].Count(c => c == ';') >= lines[0].Count(c => c == ',') ? ';' : ',';
        var header = SplitRow(lines[0], delimiter).Select(name => name.ToLowerInvariant()).ToList();
        string Cell(IReadOnlyList<string> cells, string name)
        {
            var index = header.IndexOf(name);
            return index >= 0 && index < cells.Count ? cells[index] : "";
        }

        var questions = new List<PlacementQuestion>();
        var problems = new List<string>();
        foreach (var (line, row) in lines.Skip(1).Select((line, index) => (line, index + 2)))
        {
            var cells = SplitRow(line, delimiter);
            var options = OptionColumns.Select(column => Cell(cells, column)).Where(option => option.Length > 0).ToList();
            var question = Cell(cells, "question");
            if (!Enum.TryParse<CefrLevel>(Cell(cells, "level"), ignoreCase: true, out var level)
                || question.Length == 0
                || options.Count < 2
                || !int.TryParse(Cell(cells, "correct"), out var correct)
                || correct < 1
                || correct > options.Count)
            {
                problems.Add($"Рядок {row} пропущено: потрібні рівень (A1–C), питання, 2–4 варіанти і номер правильного.");
                continue;
            }

            questions.Add(new PlacementQuestion(questions.Count + 1, level, Cell(cells, "instruction"), question, options, correct - 1));
        }

        return new PlacementTest(questions, problems);
    }

    public IReadOnlyList<PlacementQuestion> Draw(Random random) =>
        Questions
            .GroupBy(q => q.Level)
            .OrderBy(g => g.Key)
            .SelectMany(g => g.OrderBy(_ => random.Next()).Take(Quota.GetValueOrDefault(g.Key)))
            .ToList();

    public bool MatchesQuota(IEnumerable<int> questionIds)
    {
        var asked = questionIds.Distinct().Select(id => Questions.FirstOrDefault(q => q.Id == id)).ToList();
        if (asked.Any(q => q is null))
        {
            return false;
        }

        var available = Questions.GroupBy(q => q.Level).ToDictionary(g => g.Key, g => Math.Min(g.Count(), Quota.GetValueOrDefault(g.Key)));
        var counts = asked.GroupBy(q => q!.Level).ToDictionary(g => g.Key, g => g.Count());
        return available.All(pair => counts.GetValueOrDefault(pair.Key) == pair.Value) && counts.Keys.All(available.ContainsKey);
    }

    public (CefrLevel Level, IReadOnlyList<LevelScore> Scores) Evaluate(IReadOnlyDictionary<int, int> answers)
    {
        var scores = Questions
            .Where(q => answers.ContainsKey(q.Id))
            .GroupBy(q => q.Level)
            .OrderBy(g => g.Key)
            .Select(g => new LevelScore(g.Key, g.Count(q => answers[q.Id] == q.Correct), g.Count()))
            .ToList();
        var level = CefrLevel.A1;
        foreach (var score in scores)
        {
            if (score.Correct < Math.Ceiling(score.Total * PassShare))
            {
                break;
            }

            level = score.Level;
        }

        return (level, scores);
    }

    private static List<string> SplitRow(string line, char delimiter)
    {
        var cells = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == delimiter)
            {
                cells.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        cells.Add(current.ToString().Trim());
        return cells;
    }
}
