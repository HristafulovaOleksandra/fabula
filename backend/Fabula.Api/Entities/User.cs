namespace Fabula.Api.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string DisplayName { get; set; }
    public NameVisibility NameVisibility { get; set; } = NameVisibility.Private;
    public string? AvatarPath { get; set; }
    public double AvatarFocusX { get; set; } = 50;
    public double AvatarFocusY { get; set; } = 50;
    public double AvatarZoom { get; set; } = 1;
    public UserRole Role { get; set; }
    public CefrLevel? Level { get; set; }
    public List<string> FavoriteTopics { get; set; } = [];
    public int CrystalBalance { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Story> Stories { get; set; } = [];
    public List<UserWord> Words { get; set; } = [];
    public Streak? Streak { get; set; }
}
