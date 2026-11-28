using System;
using System.Collections.Generic;
using Fabula.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    [DbContext(typeof(FabulaDbContext))]
    partial class FabulaDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Fabula.Api.Entities.Assignment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateOnly?>("DueDate")
                        .HasColumnType("date");

                    b.Property<string>("Note")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<Guid>("StoryId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("StudentId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("TeacherId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("StoryId");

                    b.HasIndex("TeacherId");

                    b.HasIndex("StudentId", "StoryId")
                        .IsUnique();

                    b.ToTable("Assignments");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Background", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("ImagePath")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("OwnerId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.ToTable("Backgrounds");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Character", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Emotion")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)");

                    b.Property<bool>("Flip")
                        .HasColumnType("boolean");

                    b.Property<string>("Position")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.Property<double>("Scale")
                        .HasColumnType("double precision");

                    b.Property<Guid>("SceneId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("StoryCharacterId")
                        .HasColumnType("uuid");

                    b.Property<double>("X")
                        .HasColumnType("double precision");

                    b.Property<double>("Y")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.HasIndex("StoryCharacterId");

                    b.HasIndex("SceneId", "Position")
                        .IsUnique();

                    b.ToTable("Characters");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Choice", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid?>("NextSceneId")
                        .HasColumnType("uuid");

                    b.Property<int>("Order")
                        .HasColumnType("integer");

                    b.Property<int>("PriceCrystals")
                        .HasColumnType("integer");

                    b.Property<Guid>("SceneId")
                        .HasColumnType("uuid");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("SceneId");

                    b.ToTable("Choices");
                });

            modelBuilder.Entity("Fabula.Api.Entities.EmailChangeCode", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("Attempts")
                        .HasColumnType("integer");

                    b.Property<string>("CodeHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NewEmail")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<DateTime?>("UsedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "CreatedAt");

                    b.ToTable("EmailChangeCodes");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Follow", b =>
                {
                    b.Property<Guid>("FollowerId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("AuthorId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("FollowerId", "AuthorId");

                    b.HasIndex("AuthorId");

                    b.ToTable("Follows");
                });

            modelBuilder.Entity("Fabula.Api.Entities.GameSession", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.PrimitiveCollection<List<Guid>>("ChoiceIds")
                        .IsRequired()
                        .HasColumnType("uuid[]");

                    b.Property<int>("CrystalsEarned")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("FinishedAt")
                        .HasColumnType("timestamp with time zone");

                    b.PrimitiveCollection<List<Guid>>("ScenePath")
                        .IsRequired()
                        .HasColumnType("uuid[]");

                    b.Property<DateTime>("StartedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("StoryId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "StoryId");

                    b.ToTable("GameSessions");
                });

            modelBuilder.Entity("Fabula.Api.Entities.ImageCheck", b =>
                {
                    b.Property<string>("Path")
                        .HasColumnType("text");

                    b.Property<string>("Age")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.Property<DateTime>("CheckedAt")
                        .HasColumnType("timestamp with time zone");

                    b.PrimitiveCollection<List<string>>("Reasons")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<string>("Verdict")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.HasKey("Path");

                    b.ToTable("ImageChecks");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Line", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Level")
                        .HasMaxLength(4)
                        .HasColumnType("character varying(4)");

                    b.Property<DateTime?>("LevelCheckedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<double?>("LevelScore")
                        .HasColumnType("double precision");

                    b.Property<int>("Order")
                        .HasColumnType("integer");

                    b.Property<Guid>("SceneId")
                        .HasColumnType("uuid");

                    b.Property<string>("Speaker")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("SceneId");

                    b.ToTable("Lines");
                });

            modelBuilder.Entity("Fabula.Api.Entities.ModerationItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AuthorMessage")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ResolutionNote")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<DateTime?>("ReviewedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("ReviewedById")
                        .HasColumnType("uuid");

                    b.Property<double>("Score")
                        .HasColumnType("double precision");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(12)
                        .HasColumnType("character varying(12)");

                    b.Property<int?>("TagId")
                        .HasColumnType("integer");

                    b.Property<Guid>("TargetId")
                        .HasColumnType("uuid");

                    b.Property<string>("TargetType")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.HasKey("Id");

                    b.HasIndex("Status");

                    b.HasIndex("TargetType", "TargetId");

                    b.ToTable("ModerationItems");
                });

            modelBuilder.Entity("Fabula.Api.Entities.PasswordResetCode", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("Attempts")
                        .HasColumnType("integer");

                    b.Property<string>("CodeHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("UsedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "CreatedAt");

                    b.ToTable("PasswordResetCodes");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Scene", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<double>("BackgroundFocusX")
                        .HasColumnType("double precision");

                    b.Property<double>("BackgroundFocusY")
                        .HasColumnType("double precision");

                    b.Property<Guid?>("BackgroundId")
                        .HasColumnType("uuid");

                    b.Property<double>("BackgroundZoom")
                        .HasColumnType("double precision");

                    b.Property<int>("Order")
                        .HasColumnType("integer");

                    b.Property<Guid>("StoryId")
                        .HasColumnType("uuid");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("BackgroundId");

                    b.HasIndex("StoryId");

                    b.ToTable("Scenes");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Story", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AgeRating")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.PrimitiveCollection<List<string>>("AllowedWords")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.PrimitiveCollection<List<string>>("ApprovedContent")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<Guid>("AuthorId")
                        .HasColumnType("uuid");

                    b.Property<double>("CoverFocusX")
                        .HasColumnType("double precision");

                    b.Property<double>("CoverFocusY")
                        .HasColumnType("double precision");

                    b.Property<string>("CoverImagePath")
                        .HasColumnType("text");

                    b.Property<double>("CoverZoom")
                        .HasColumnType("double precision");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Level")
                        .IsRequired()
                        .HasMaxLength(4)
                        .HasColumnType("character varying(4)");

                    b.Property<string>("ModerationNote")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)");

                    b.Property<int>("PriceCrystals")
                        .HasColumnType("integer");

                    b.Property<int>("RatingCount")
                        .HasColumnType("integer");

                    b.Property<int>("RatingSum")
                        .HasColumnType("integer");

                    b.Property<Guid?>("StartSceneId")
                        .HasColumnType("uuid");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(24)
                        .HasColumnType("character varying(24)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Visibility")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.HasKey("Id");

                    b.HasIndex("AuthorId");

                    b.HasIndex("Status", "Level");

                    b.ToTable("Stories");
                });

            modelBuilder.Entity("Fabula.Api.Entities.StoryCharacter", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(60)
                        .HasColumnType("character varying(60)");

                    b.Property<int>("Order")
                        .HasColumnType("integer");

                    b.Property<Guid>("StoryId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("StoryId", "Name")
                        .IsUnique();

                    b.ToTable("StoryCharacters");
                });

            modelBuilder.Entity("Fabula.Api.Entities.StoryCharacterState", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ImagePath")
                        .HasColumnType("text");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)");

                    b.Property<int>("Order")
                        .HasColumnType("integer");

                    b.Property<Guid>("StoryCharacterId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("StoryCharacterId", "Key")
                        .IsUnique();

                    b.ToTable("StoryCharacterStates");
                });

            modelBuilder.Entity("Fabula.Api.Entities.StoryRating", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("StoryId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("Stars")
                        .HasColumnType("integer");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("UserId", "StoryId");

                    b.HasIndex("StoryId");

                    b.ToTable("StoryRatings");
                });

            modelBuilder.Entity("Fabula.Api.Entities.StoryWord", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("StoryId")
                        .HasColumnType("uuid");

                    b.Property<string>("Translation")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Word")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("StoryId");

                    b.ToTable("StoryWords");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Streak", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<int>("CurrentDays")
                        .HasColumnType("integer");

                    b.Property<DateOnly?>("LastActiveDate")
                        .HasColumnType("date");

                    b.Property<int>("LongestDays")
                        .HasColumnType("integer");

                    b.Property<int>("RescuableDays")
                        .HasColumnType("integer");

                    b.Property<DateOnly?>("RescuableUntil")
                        .HasColumnType("date");

                    b.HasKey("UserId");

                    b.ToTable("Streaks");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Guid?>("CreatedById")
                        .HasColumnType("uuid");

                    b.Property<string>("Kind")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(12)
                        .HasColumnType("character varying(12)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Tags");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Kind = "Genre",
                            Name = "Adventure",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 2,
                            Kind = "Genre",
                            Name = "Mystery",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 3,
                            Kind = "Genre",
                            Name = "Comedy",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 4,
                            Kind = "Genre",
                            Name = "Romance",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 5,
                            Kind = "Genre",
                            Name = "Fantasy",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 6,
                            Kind = "Genre",
                            Name = "Science fiction",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 7,
                            Kind = "Genre",
                            Name = "Everyday life",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 8,
                            Kind = "Topic",
                            Name = "Travel",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 9,
                            Kind = "Topic",
                            Name = "School",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 10,
                            Kind = "Topic",
                            Name = "Work",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 11,
                            Kind = "Topic",
                            Name = "Food",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 12,
                            Kind = "Topic",
                            Name = "Family",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 13,
                            Kind = "Topic",
                            Name = "Friends",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 14,
                            Kind = "Topic",
                            Name = "Shopping",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 15,
                            Kind = "Topic",
                            Name = "Health",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 16,
                            Kind = "Topic",
                            Name = "Sport",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 17,
                            Kind = "Topic",
                            Name = "Nature",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 18,
                            Kind = "Topic",
                            Name = "Technology",
                            Status = "Approved"
                        },
                        new
                        {
                            Id = 19,
                            Kind = "Topic",
                            Name = "Culture",
                            Status = "Approved"
                        });
                });

            modelBuilder.Entity("Fabula.Api.Entities.TeachingLink", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("AcceptedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(12)
                        .HasColumnType("character varying(12)");

                    b.Property<Guid>("StudentId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("TeacherId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("StudentId");

                    b.HasIndex("TeacherId", "StudentId")
                        .IsUnique();

                    b.ToTable("TeachingLinks");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Transaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("Amount")
                        .HasColumnType("integer");

                    b.Property<Guid?>("ChoiceId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasMaxLength(24)
                        .HasColumnType("character varying(24)");

                    b.Property<Guid?>("StoryId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "CreatedAt");

                    b.HasIndex("UserId", "StoryId", "Reason");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("Fabula.Api.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<double>("AvatarFocusX")
                        .HasColumnType("double precision");

                    b.Property<double>("AvatarFocusY")
                        .HasColumnType("double precision");

                    b.Property<string>("AvatarPath")
                        .HasColumnType("text");

                    b.Property<double>("AvatarZoom")
                        .HasColumnType("double precision");

                    b.Property<DateOnly?>("BirthDate")
                        .HasColumnType("date");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("CrystalBalance")
                        .HasColumnType("integer");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.PrimitiveCollection<List<string>>("FavoriteTopics")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<bool>("IsBlocked")
                        .HasColumnType("boolean");

                    b.Property<string>("Level")
                        .HasMaxLength(4)
                        .HasColumnType("character varying(4)");

                    b.Property<string>("NameVisibility")
                        .IsRequired()
                        .HasMaxLength(12)
                        .HasColumnType("character varying(12)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(30)
                        .HasColumnType("character varying(30)");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Fabula.Api.Entities.UserWord", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("CorrectInRow")
                        .HasColumnType("integer");

                    b.Property<DateTime>("FirstSeenAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("HistoryCorrect")
                        .HasColumnType("integer");

                    b.Property<int>("HistorySeen")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("LastPracticedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("LastSeenAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime?>("LearnedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("LearningLanguage")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Lemma")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<bool>("MarkedKnown")
                        .HasColumnType("boolean");

                    b.Property<int>("SessionCorrect")
                        .HasColumnType("integer");

                    b.Property<int>("SessionSeen")
                        .HasColumnType("integer");

                    b.Property<Guid?>("SourceStoryId")
                        .HasColumnType("uuid");

                    b.Property<string>("SurfaceForm")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("TimesTapped")
                        .HasColumnType("integer");

                    b.Property<string>("Translation")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("UiLanguage")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "Lemma")
                        .IsUnique();

                    b.ToTable("UserWords");
                });

            modelBuilder.Entity("StoryTag", b =>
                {
                    b.Property<Guid>("StoriesId")
                        .HasColumnType("uuid");

                    b.Property<int>("TagsId")
                        .HasColumnType("integer");

                    b.HasKey("StoriesId", "TagsId");

                    b.HasIndex("TagsId");

                    b.ToTable("StoryTags", (string)null);
                });

            modelBuilder.Entity("Fabula.Api.Entities.Assignment", b =>
                {
                    b.HasOne("Fabula.Api.Entities.Story", null)
                        .WithMany()
                        .HasForeignKey("StoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fabula.Api.Entities.User", null)
                        .WithMany()
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fabula.Api.Entities.User", null)
                        .WithMany()
                        .HasForeignKey("TeacherId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.Character", b =>
                {
                    b.HasOne("Fabula.Api.Entities.Scene", null)
                        .WithMany("Characters")
                        .HasForeignKey("SceneId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fabula.Api.Entities.StoryCharacter", "StoryCharacter")
                        .WithMany()
                        .HasForeignKey("StoryCharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("StoryCharacter");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Choice", b =>
                {
                    b.HasOne("Fabula.Api.Entities.Scene", null)
                        .WithMany("Choices")
                        .HasForeignKey("SceneId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.EmailChangeCode", b =>
                {
                    b.HasOne("Fabula.Api.Entities.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.Follow", b =>
                {
                    b.HasOne("Fabula.Api.Entities.User", "Author")
                        .WithMany()
                        .HasForeignKey("AuthorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fabula.Api.Entities.User", null)
                        .WithMany()
                        .HasForeignKey("FollowerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Author");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Line", b =>
                {
                    b.HasOne("Fabula.Api.Entities.Scene", null)
                        .WithMany("Lines")
                        .HasForeignKey("SceneId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.PasswordResetCode", b =>
                {
                    b.HasOne("Fabula.Api.Entities.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.Scene", b =>
                {
                    b.HasOne("Fabula.Api.Entities.Background", "Background")
                        .WithMany()
                        .HasForeignKey("BackgroundId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Fabula.Api.Entities.Story", "Story")
                        .WithMany("Scenes")
                        .HasForeignKey("StoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Background");

                    b.Navigation("Story");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Story", b =>
                {
                    b.HasOne("Fabula.Api.Entities.User", "Author")
                        .WithMany("Stories")
                        .HasForeignKey("AuthorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Author");
                });

            modelBuilder.Entity("Fabula.Api.Entities.StoryCharacter", b =>
                {
                    b.HasOne("Fabula.Api.Entities.Story", null)
                        .WithMany("Cast")
                        .HasForeignKey("StoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.StoryCharacterState", b =>
                {
                    b.HasOne("Fabula.Api.Entities.StoryCharacter", null)
                        .WithMany("States")
                        .HasForeignKey("StoryCharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.StoryRating", b =>
                {
                    b.HasOne("Fabula.Api.Entities.Story", null)
                        .WithMany()
                        .HasForeignKey("StoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fabula.Api.Entities.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.StoryWord", b =>
                {
                    b.HasOne("Fabula.Api.Entities.Story", null)
                        .WithMany("Words")
                        .HasForeignKey("StoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.Streak", b =>
                {
                    b.HasOne("Fabula.Api.Entities.User", null)
                        .WithOne("Streak")
                        .HasForeignKey("Fabula.Api.Entities.Streak", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.TeachingLink", b =>
                {
                    b.HasOne("Fabula.Api.Entities.User", "Student")
                        .WithMany()
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fabula.Api.Entities.User", "Teacher")
                        .WithMany()
                        .HasForeignKey("TeacherId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Student");

                    b.Navigation("Teacher");
                });

            modelBuilder.Entity("Fabula.Api.Entities.UserWord", b =>
                {
                    b.HasOne("Fabula.Api.Entities.User", null)
                        .WithMany("Words")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("StoryTag", b =>
                {
                    b.HasOne("Fabula.Api.Entities.Story", null)
                        .WithMany()
                        .HasForeignKey("StoriesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Fabula.Api.Entities.Tag", null)
                        .WithMany()
                        .HasForeignKey("TagsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Fabula.Api.Entities.Scene", b =>
                {
                    b.Navigation("Characters");

                    b.Navigation("Choices");

                    b.Navigation("Lines");
                });

            modelBuilder.Entity("Fabula.Api.Entities.Story", b =>
                {
                    b.Navigation("Cast");

                    b.Navigation("Scenes");

                    b.Navigation("Words");
                });

            modelBuilder.Entity("Fabula.Api.Entities.StoryCharacter", b =>
                {
                    b.Navigation("States");
                });

            modelBuilder.Entity("Fabula.Api.Entities.User", b =>
                {
                    b.Navigation("Stories");

                    b.Navigation("Streak");

                    b.Navigation("Words");
                });
#pragma warning restore 612, 618
        }
    }
}
