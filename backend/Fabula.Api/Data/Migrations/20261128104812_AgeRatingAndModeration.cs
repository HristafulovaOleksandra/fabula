using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class AgeRatingAndModeration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "Users",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgeRating",
                table: "Stories",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "All");

            migrationBuilder.AddColumn<string>(
                name: "ModerationNote",
                table: "Stories",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Stories",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "Public");

            migrationBuilder.AddColumn<string>(
                name: "AuthorMessage",
                table: "ModerationItems",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "ModerationItems",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ImageChecks",
                columns: table => new
                {
                    Path = table.Column<string>(type: "text", nullable: false),
                    Verdict = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Age = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Reasons = table.Column<List<string>>(type: "text[]", nullable: false),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageChecks", x => x.Path);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationItems_TargetType_TargetId",
                table: "ModerationItems",
                columns: new[] { "TargetType", "TargetId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageChecks");

            migrationBuilder.DropIndex(
                name: "IX_ModerationItems_TargetType_TargetId",
                table: "ModerationItems");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AgeRating",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "ModerationNote",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "AuthorMessage",
                table: "ModerationItems");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "ModerationItems");
        }
    }
}
