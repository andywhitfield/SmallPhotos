using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallPhotos.Migrations
{
    public partial class UserAccount_GalleryPageSize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GalleryImagePageSize",
                table: "UserAccounts",
                type: "INTEGER",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GalleryImagePageSize",
                table: "UserAccounts");
        }
    }
}
