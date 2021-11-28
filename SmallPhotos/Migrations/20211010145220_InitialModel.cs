using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SmallPhotos.Migrations
{
    public partial class InitialModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    UserAccountId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuthenticationUri = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.UserAccountId);
                });

            migrationBuilder.CreateTable(
                name: "AlbumSources",
                columns: table => new
                {
                    AlbumSourceId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserAccountId = table.Column<long>(type: "INTEGER", nullable: false),
                    Folder = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumSources", x => x.AlbumSourceId);
                    table.ForeignKey(
                        name: "FK_AlbumSources_UserAccounts_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "UserAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    PhotoId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlbumSourceId = table.Column<long>(type: "INTEGER", nullable: false),
                    Filename = table.Column<string>(type: "TEXT", nullable: true),
                    FileCreationDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FileModificationDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdateDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.PhotoId);
                    table.ForeignKey(
                        name: "FK_Photos_AlbumSources_AlbumSourceId",
                        column: x => x.AlbumSourceId,
                        principalTable: "AlbumSources",
                        principalColumn: "AlbumSourceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Thumbnails",
                columns: table => new
                {
                    ThumbnailId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhotoId = table.Column<long>(type: "INTEGER", nullable: false),
                    ThumbnailImage = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ThumbnailSize = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Thumbnails", x => x.ThumbnailId);
                    table.ForeignKey(
                        name: "FK_Thumbnails_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "PhotoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlbumSources_UserAccountId",
                table: "AlbumSources",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_AlbumSourceId",
                table: "Photos",
                column: "AlbumSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Thumbnails_PhotoId",
                table: "Thumbnails",
                column: "PhotoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Thumbnails");

            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "AlbumSources");

            migrationBuilder.DropTable(
                name: "UserAccounts");
        }
    }
}
