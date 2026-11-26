using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class AvatarFraming : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AvatarFocusX",
                table: "Users",
                type: "double precision",
                nullable: false,
                defaultValue: 50.0);

            migrationBuilder.AddColumn<double>(
                name: "AvatarFocusY",
                table: "Users",
                type: "double precision",
                nullable: false,
                defaultValue: 50.0);

            migrationBuilder.AddColumn<double>(
                name: "AvatarZoom",
                table: "Users",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarFocusX",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarFocusY",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarZoom",
                table: "Users");
        }
    }
}
