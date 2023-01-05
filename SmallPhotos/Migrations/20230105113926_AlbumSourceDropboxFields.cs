using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallPhotos.Migrations
{
    /// <inheritdoc />
    public partial class AlbumSourceDropboxFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DropboxAccessToken",
                table: "AlbumSources",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropboxRefreshToken",
                table: "AlbumSources",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropboxAccessToken",
                table: "AlbumSources");

            migrationBuilder.DropColumn(
                name: "DropboxRefreshToken",
                table: "AlbumSources");
        }
    }
}
