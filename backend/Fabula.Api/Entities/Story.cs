namespace Fabula.Api.Entities;

public class Story
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public User? Author { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string? CoverImagePath { get; set; }
    public CefrLevel Level { get; set; }
    public int PriceCrystals { get; set; }
    public StoryStatus Status { get; set; }
    public Guid? StartSceneId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Tag> Tags { get; set; } = [];
    public List<Scene> Scenes { get; set; } = [];
    public List<StoryWord> Words { get; set; } = [];
}

public class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public TagKind Kind { get; set; }

    public List<Story> Stories { get; set; } = [];
}

public class StoryWord
{
    public Guid Id { get; set; }
    public Guid StoryId { get; set; }
    public required string Word { get; set; }
    public required string Translation { get; set; }
}
