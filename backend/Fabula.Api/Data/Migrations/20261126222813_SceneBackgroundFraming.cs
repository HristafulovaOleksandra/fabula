using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class SceneBackgroundFraming : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BackgroundFocusX",
                table: "Scenes",
                type: "double precision",
                nullable: false,
                defaultValue: 50.0);

            migrationBuilder.AddColumn<double>(
                name: "BackgroundFocusY",
                table: "Scenes",
                type: "double precision",
                nullable: false,
                defaultValue: 50.0);

            migrationBuilder.AddColumn<double>(
                name: "BackgroundZoom",
                table: "Scenes",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackgroundFocusX",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "BackgroundFocusY",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "BackgroundZoom",
                table: "Scenes");
        }
    }
}
