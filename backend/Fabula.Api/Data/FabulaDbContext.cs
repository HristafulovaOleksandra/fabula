using Fabula.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fabula.Api.Data;

public class FabulaDbContext(DbContextOptions<FabulaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<StoryWord> StoryWords => Set<StoryWord>();
    public DbSet<Background> Backgrounds => Set<Background>();
    public DbSet<Scene> Scenes => Set<Scene>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<StoryCharacter> StoryCharacters => Set<StoryCharacter>();
    public DbSet<StoryCharacterState> StoryCharacterStates => Set<StoryCharacterState>();
    public DbSet<Line> Lines => Set<Line>();
    public DbSet<Choice> Choices => Set<Choice>();
    public DbSet<UserWord> UserWords => Set<UserWord>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Streak> Streaks => Set<Streak>();
    public DbSet<TeachingLink> TeachingLinks => Set<TeachingLink>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
    public DbSet<EmailChangeCode> EmailChangeCodes => Set<EmailChangeCode>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<ModerationItem> ModerationItems => Set<ModerationItem>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(16);
        builder.Properties<CefrLevel>().HaveConversion<string>().HaveMaxLength(4);
        builder.Properties<StoryStatus>().HaveConversion<string>().HaveMaxLength(24);
        builder.Properties<TagKind>().HaveConversion<string>().HaveMaxLength(8);
        builder.Properties<CharacterPosition>().HaveConversion<string>().HaveMaxLength(8);
        builder.Properties<TransactionReason>().HaveConversion<string>().HaveMaxLength(24);
        builder.Properties<ModerationTarget>().HaveConversion<string>().HaveMaxLength(8);
        builder.Properties<ModerationStatus>().HaveConversion<string>().HaveMaxLength(12);
        builder.Properties<TeachingStatus>().HaveConversion<string>().HaveMaxLength(12);
        builder.Properties<NameVisibility>().HaveConversion<string>().HaveMaxLength(12);
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>(user =>
        {
            user.HasIndex(u => u.Email).IsUnique();
            user.HasIndex(u => u.Username).IsUnique();
            user.Property(u => u.Username).HasMaxLength(30);
            user.Property(u => u.Email).HasMaxLength(256);
            user.Property(u => u.DisplayName).HasMaxLength(100);
            user.HasOne(u => u.Streak).WithOne().HasForeignKey<Streak>(s => s.UserId);
            user.HasMany(u => u.Words).WithOne().HasForeignKey(w => w.UserId);
        });

        model.Entity<Story>(story =>
        {
            story.Property(s => s.Title).HasMaxLength(200);
            story.HasOne(s => s.Author).WithMany(u => u.Stories).HasForeignKey(s => s.AuthorId);
            story.HasMany(s => s.Tags).WithMany(t => t.Stories).UsingEntity(join => join.ToTable("StoryTags"));
            story.HasMany(s => s.Scenes).WithOne(s => s.Story).HasForeignKey(s => s.StoryId);
            story.HasMany(s => s.Words).WithOne().HasForeignKey(w => w.StoryId);
            story.HasMany(s => s.Cast).WithOne().HasForeignKey(c => c.StoryId);
            story.HasIndex(s => new { s.Status, s.Level });
        });

        model.Entity<Tag>(tag =>
        {
            tag.HasIndex(t => t.Name).IsUnique();
            tag.Property(t => t.Name).HasMaxLength(50);
            tag.HasData(SeedTags());
        });

        model.Entity<Scene>(scene =>
        {
            scene.HasMany(s => s.Characters).WithOne().HasForeignKey(c => c.SceneId);
            scene.HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.SceneId);
            scene.HasMany(s => s.Choices).WithOne().HasForeignKey(c => c.SceneId);
            scene.HasOne(s => s.Background).WithMany().HasForeignKey(s => s.BackgroundId).OnDelete(DeleteBehavior.SetNull);
        });

        model.Entity<Character>(character =>
        {
            character.HasIndex(c => new { c.SceneId, c.Position }).IsUnique();
            character.HasOne(c => c.StoryCharacter).WithMany().HasForeignKey(c => c.StoryCharacterId);
            character.Property(c => c.Emotion).HasMaxLength(30);
        });

        model.Entity<StoryCharacter>(character =>
        {
            character.Property(c => c.Name).HasMaxLength(60);
            character.HasIndex(c => new { c.StoryId, c.Name }).IsUnique();
            character.HasMany(c => c.States).WithOne().HasForeignKey(s => s.StoryCharacterId);
        });

        model.Entity<StoryCharacterState>(state =>
        {
            state.Property(s => s.Key).HasMaxLength(30);
            state.HasIndex(s => new { s.StoryCharacterId, s.Key }).IsUnique();
        });
        model.Entity<UserWord>().HasIndex(w => new { w.UserId, w.Lemma }).IsUnique();
        model.Entity<Streak>().HasKey(s => s.UserId);
        model.Entity<Transaction>().HasIndex(t => new { t.UserId, t.CreatedAt });
        model.Entity<GameSession>().HasIndex(g => new { g.UserId, g.StoryId });

        model.Entity<TeachingLink>(link =>
        {
            link.HasIndex(l => new { l.TeacherId, l.StudentId }).IsUnique();
            link.HasOne(l => l.Teacher).WithMany().HasForeignKey(l => l.TeacherId);
            link.HasOne(l => l.Student).WithMany().HasForeignKey(l => l.StudentId);
        });

        model.Entity<Follow>(follow =>
        {
            follow.HasKey(f => new { f.FollowerId, f.AuthorId });
            follow.HasOne<User>().WithMany().HasForeignKey(f => f.FollowerId);
            follow.HasOne(f => f.Author).WithMany().HasForeignKey(f => f.AuthorId);
            follow.HasIndex(f => f.AuthorId);
        });

        model.Entity<PasswordResetCode>(code =>
        {
            code.HasOne<User>().WithMany().HasForeignKey(c => c.UserId);
            code.HasIndex(c => new { c.UserId, c.CreatedAt });
        });

        model.Entity<EmailChangeCode>(code =>
        {
            code.HasOne<User>().WithMany().HasForeignKey(c => c.UserId);
            code.HasIndex(c => new { c.UserId, c.CreatedAt });
            code.Property(c => c.NewEmail).HasMaxLength(256);
        });

        model.Entity<Assignment>(assignment =>
        {
            assignment.HasIndex(a => new { a.StudentId, a.StoryId }).IsUnique();
            assignment.HasOne<User>().WithMany().HasForeignKey(a => a.TeacherId);
            assignment.HasOne<User>().WithMany().HasForeignKey(a => a.StudentId);
            assignment.HasOne<Story>().WithMany().HasForeignKey(a => a.StoryId);
        });

        model.Entity<ModerationItem>().HasIndex(m => m.Status);
    }

    private static Tag[] SeedTags()
    {
        string[] genres = ["Adventure", "Mystery", "Comedy", "Romance", "Fantasy", "Science fiction", "Everyday life"];
        string[] topics = ["Travel", "School", "Work", "Food", "Family", "Friends", "Shopping", "Health", "Sport", "Nature", "Technology", "Culture"];
        var id = 1;
        return
        [
            .. genres.Select(name => new Tag { Id = id++, Name = name, Kind = TagKind.Genre }),
            .. topics.Select(name => new Tag { Id = id++, Name = name, Kind = TagKind.Topic })
        ];
    }
}
