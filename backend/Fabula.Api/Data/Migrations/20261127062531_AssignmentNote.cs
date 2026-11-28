using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class AssignmentNote : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Assignments",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "Assignments");
        }
    }
}
