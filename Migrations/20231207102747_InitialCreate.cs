using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace humidity_api_minimal.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HumidityDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    sensorId = table.Column<int>(type: "INTEGER", nullable: false),
                    humidity = table.Column<int>(type: "INTEGER", nullable: false),
                    dateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumidityDatas", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HumidityDatas");
        }
    }
}
