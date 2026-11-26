using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class RerateLinesBySentence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "Lines" SET "Level" = NULL, "LevelScore" = NULL, "LevelCheckedAt" = NULL;""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
