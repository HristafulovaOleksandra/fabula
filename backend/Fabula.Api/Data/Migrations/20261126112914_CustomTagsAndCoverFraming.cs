using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabula.Api.Data.Migrations
{
    public partial class CustomTagsAndCoverFraming : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "Tags",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Tags",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<double>(
                name: "CoverFocusX",
                table: "Stories",
                type: "double precision",
                nullable: false,
                defaultValue: 50.0);

            migrationBuilder.AddColumn<double>(
                name: "CoverFocusY",
                table: "Stories",
                type: "double precision",
                nullable: false,
                defaultValue: 50.0);

            migrationBuilder.AddColumn<double>(
                name: "CoverZoom",
                table: "Stories",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "CreatedById", "Status" },
                values: new object[] { null, "Approved" });

            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('\"Tags\"', 'Id'), (SELECT MAX(\"Id\") FROM \"Tags\"));");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CoverFocusX",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "CoverFocusY",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "CoverZoom",
                table: "Stories");
        }
    }
}
