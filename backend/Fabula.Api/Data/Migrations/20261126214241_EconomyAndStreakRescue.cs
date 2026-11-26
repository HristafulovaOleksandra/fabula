using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class EconomyAndStreakRescue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FreezesAvailable",
                table: "Streaks",
                newName: "RescuableDays");

            migrationBuilder.AddColumn<Guid>(
                name: "ChoiceId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RescuableUntil",
                table: "Streaks",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_StoryId_Reason",
                table: "Transactions",
                columns: new[] { "UserId", "StoryId", "Reason" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId_StoryId_Reason",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ChoiceId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RescuableUntil",
                table: "Streaks");

            migrationBuilder.RenameColumn(
                name: "RescuableDays",
                table: "Streaks",
                newName: "FreezesAvailable");
        }
    }
}
