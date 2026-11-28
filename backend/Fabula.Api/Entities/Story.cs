namespace Fabula.Api.Entities;

public class Story
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string? CoverImagePath { get; set; }
    public double CoverFocusX { get; set; } = 50;
    public double CoverFocusY { get; set; } = 50;
    public double CoverZoom { get; set; } = 1;
    public CefrLevel Level { get; set; }
    public int PriceCrystals { get; set; }
    public StoryStatus Status { get; set; }
    public Guid? StartSceneId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int RatingCount { get; set; }
    public int RatingSum { get; set; }

    public List<Tag> Tags { get; set; } = [];
    public List<Scene> Scenes { get; set; } = [];
    public List<StoryCharacter> Cast { get; set; } = [];
    public List<StoryWord> Words { get; set; } = [];
    public List<string> AllowedWords { get; set; } = [];
}

public class StoryRating
{
    public Guid UserId { get; set; }
    public Guid StoryId { get; set; }
    public int Stars { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public TagKind Kind { get; set; }
    public ModerationStatus Status { get; set; } = ModerationStatus.Approved;
    public Guid? CreatedById { get; set; }

    public List<Story> Stories { get; set; } = [];
}

public class StoryWord
{
    public Guid Id { get; set; }
    public Guid StoryId { get; set; }
    public required string Word { get; set; }
    public required string Translation { get; set; }
}
