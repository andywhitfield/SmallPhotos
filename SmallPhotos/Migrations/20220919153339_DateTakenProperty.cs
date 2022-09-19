using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallPhotos.Migrations
{
    public partial class DateTakenProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateTaken",
                table: "Photos",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateTaken",
                table: "Photos");
        }
    }
}
