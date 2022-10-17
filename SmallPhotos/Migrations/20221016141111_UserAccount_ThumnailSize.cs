using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallPhotos.Migrations
{
    public partial class UserAccount_ThumnailSize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ThumbnailSize",
                table: "UserAccounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailSize",
                table: "UserAccounts");
        }
    }
}
