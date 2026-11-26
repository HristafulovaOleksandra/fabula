using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class RerateCLines : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "Lines" SET "Level" = NULL, "LevelScore" = NULL, "LevelCheckedAt" = NULL WHERE "Level" = 'C';""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
