using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class AdminModeration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "ApprovedContent",
                table: "Stories",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int>(
                name: "TagId",
                table: "ModerationItems",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""UPDATE "ModerationItems" SET "TagId" = "Score"::integer WHERE "TargetType" = 'Tag';""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovedContent",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "TagId",
                table: "ModerationItems");
        }
    }
}
