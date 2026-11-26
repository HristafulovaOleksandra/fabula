namespace Fabula.Api.Entities;

public class Background
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string ImagePath { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Scene
{
    public Guid Id { get; set; }
    public Guid StoryId { get; set; }
    public Story? Story { get; set; }
    public required string Title { get; set; }
    public int Order { get; set; }
    public Guid? BackgroundId { get; set; }
    public Background? Background { get; set; }

    public List<Character> Characters { get; set; } = [];
    public List<Line> Lines { get; set; } = [];
    public List<Choice> Choices { get; set; } = [];
}

public static class Emotions
{
    public const string Neutral = "neutral";
    public const string Happy = "happy";
    public const string Sad = "sad";
    public const string Surprised = "surprised";
    public static readonly string[] All = [Neutral, Happy, Sad, Surprised];
}

public class StoryCharacter
{
    public Guid Id { get; set; }
    public Guid StoryId { get; set; }
    public required string Name { get; set; }
    public int Order { get; set; }

    public List<StoryCharacterState> States { get; set; } = [];

    public string? ImageFor(string emotion) =>
        States.FirstOrDefault(s => s.Key == emotion)?.ImagePath
        ?? States.FirstOrDefault(s => s.Key == Emotions.Neutral)?.ImagePath;
}

public class StoryCharacterState
{
    public Guid Id { get; set; }
    public Guid StoryCharacterId { get; set; }
    public required string Key { get; set; }
    public string? ImagePath { get; set; }
    public int Order { get; set; }
}

public static class Stage
{
    public const double LeftX = 25;
    public const double RightX = 75;
}

public class Character
{
    public Guid Id { get; set; }
    public Guid SceneId { get; set; }
    public Guid StoryCharacterId { get; set; }
    public StoryCharacter? StoryCharacter { get; set; }
    public CharacterPosition Position { get; set; }
    public string Emotion { get; set; } = Emotions.Neutral;
    public double X { get; set; } = Stage.LeftX;
    public double Y { get; set; }
    public double Scale { get; set; } = 1;
    public bool Flip { get; set; }
}

public class Line
{
    public Guid Id { get; set; }
    public Guid SceneId { get; set; }
    public int Order { get; set; }
    public required string Speaker { get; set; }
    public required string Text { get; set; }
    public CefrLevel? Level { get; set; }
    public double? LevelScore { get; set; }
    public DateTime? LevelCheckedAt { get; set; }
}

public class Choice
{
    public Guid Id { get; set; }
    public Guid SceneId { get; set; }
    public int Order { get; set; }
    public required string Text { get; set; }
    public Guid? NextSceneId { get; set; }
    public int PriceCrystals { get; set; }
}
