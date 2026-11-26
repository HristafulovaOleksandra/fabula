using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class StoryCast : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"Characters\";");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Characters");

            migrationBuilder.AlterColumn<string>(
                name: "Emotion",
                table: "Characters",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "StoryCharacterId",
                table: "Characters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "StoryCharacters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    ImageNeutral = table.Column<string>(type: "text", nullable: true),
                    ImageHappy = table.Column<string>(type: "text", nullable: true),
                    ImageSad = table.Column<string>(type: "text", nullable: true),
                    ImageSurprised = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryCharacters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryCharacters_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_StoryCharacterId",
                table: "Characters",
                column: "StoryCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryCharacters_StoryId_Name",
                table: "StoryCharacters",
                columns: new[] { "StoryId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_StoryCharacters_StoryCharacterId",
                table: "Characters",
                column: "StoryCharacterId",
                principalTable: "StoryCharacters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_StoryCharacters_StoryCharacterId",
                table: "Characters");

            migrationBuilder.DropTable(
                name: "StoryCharacters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_StoryCharacterId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "StoryCharacterId",
                table: "Characters");

            migrationBuilder.AlterColumn<string>(
                name: "Emotion",
                table: "Characters",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Characters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
