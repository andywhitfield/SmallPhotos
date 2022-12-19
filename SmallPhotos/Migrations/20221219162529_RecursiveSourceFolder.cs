using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallPhotos.Migrations
{
    /// <inheritdoc />
    public partial class RecursiveSourceFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelativePath",
                table: "Photos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecurseSubFolders",
                table: "AlbumSources",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelativePath",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "RecurseSubFolders",
                table: "AlbumSources");
        }
    }
}
