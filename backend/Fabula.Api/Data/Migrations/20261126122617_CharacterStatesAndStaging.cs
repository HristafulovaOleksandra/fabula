using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class CharacterStatesAndStaging : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoryCharacterStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ImagePath = table.Column<string>(type: "text", nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryCharacterStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryCharacterStates_StoryCharacters_StoryCharacterId",
                        column: x => x.StoryCharacterId,
                        principalTable: "StoryCharacters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoryCharacterStates_StoryCharacterId_Key",
                table: "StoryCharacterStates",
                columns: new[] { "StoryCharacterId", "Key" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "StoryCharacterStates" ("Id", "StoryCharacterId", "Key", "ImagePath", "Order")
                SELECT gen_random_uuid(), c."Id", s.key, s.path, s.ord
                FROM "StoryCharacters" c
                CROSS JOIN LATERAL (VALUES
                    ('neutral', c."ImageNeutral", 0),
                    ('happy', c."ImageHappy", 1),
                    ('sad', c."ImageSad", 2),
                    ('surprised', c."ImageSurprised", 3)) AS s(key, path, ord)
                WHERE s.path IS NOT NULL;
                """);

            migrationBuilder.DropColumn(name: "ImageHappy", table: "StoryCharacters");
            migrationBuilder.DropColumn(name: "ImageNeutral", table: "StoryCharacters");
            migrationBuilder.DropColumn(name: "ImageSad", table: "StoryCharacters");
            migrationBuilder.DropColumn(name: "ImageSurprised", table: "StoryCharacters");

            migrationBuilder.AlterColumn<string>(
                name: "Emotion",
                table: "Characters",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<bool>(name: "Flip", table: "Characters", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<double>(name: "Scale", table: "Characters", type: "double precision", nullable: false, defaultValue: 1.0);
            migrationBuilder.AddColumn<double>(name: "X", table: "Characters", type: "double precision", nullable: false, defaultValue: 25.0);
            migrationBuilder.AddColumn<double>(name: "Y", table: "Characters", type: "double precision", nullable: false, defaultValue: 0.0);

            migrationBuilder.Sql("""UPDATE "Characters" SET "X" = 75 WHERE "Position" = 'Right';""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "ImageHappy", table: "StoryCharacters", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ImageNeutral", table: "StoryCharacters", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ImageSad", table: "StoryCharacters", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ImageSurprised", table: "StoryCharacters", type: "text", nullable: true);

            migrationBuilder.Sql("""
                UPDATE "StoryCharacters" c SET
                    "ImageNeutral" = (SELECT "ImagePath" FROM "StoryCharacterStates" WHERE "StoryCharacterId" = c."Id" AND "Key" = 'neutral'),
                    "ImageHappy" = (SELECT "ImagePath" FROM "StoryCharacterStates" WHERE "StoryCharacterId" = c."Id" AND "Key" = 'happy'),
                    "ImageSad" = (SELECT "ImagePath" FROM "StoryCharacterStates" WHERE "StoryCharacterId" = c."Id" AND "Key" = 'sad'),
                    "ImageSurprised" = (SELECT "ImagePath" FROM "StoryCharacterStates" WHERE "StoryCharacterId" = c."Id" AND "Key" = 'surprised');
                """);

            migrationBuilder.DropTable(name: "StoryCharacterStates");
            migrationBuilder.DropColumn(name: "Flip", table: "Characters");
            migrationBuilder.DropColumn(name: "Scale", table: "Characters");
            migrationBuilder.DropColumn(name: "X", table: "Characters");
            migrationBuilder.DropColumn(name: "Y", table: "Characters");

            migrationBuilder.Sql("""UPDATE "Characters" SET "Emotion" = 'neutral' WHERE length("Emotion") > 16;""");
            migrationBuilder.AlterColumn<string>(
                name: "Emotion",
                table: "Characters",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }
    }
}
