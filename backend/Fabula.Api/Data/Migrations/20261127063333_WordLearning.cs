using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class WordLearning : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrectInRow",
                table: "UserWords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPracticedAt",
                table: "UserWords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LearnedAt",
                table: "UserWords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MarkedKnown",
                table: "UserWords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TimesTapped",
                table: "UserWords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""UPDATE "UserWords" SET "TimesTapped" = "HistorySeen", "HistorySeen" = 0, "HistoryCorrect" = 0, "SessionSeen" = 0, "SessionCorrect" = 0;""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectInRow",
                table: "UserWords");

            migrationBuilder.DropColumn(
                name: "LastPracticedAt",
                table: "UserWords");

            migrationBuilder.DropColumn(
                name: "LearnedAt",
                table: "UserWords");

            migrationBuilder.DropColumn(
                name: "MarkedKnown",
                table: "UserWords");

            migrationBuilder.DropColumn(
                name: "TimesTapped",
                table: "UserWords");
        }
    }
}
