using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallPhotos.Migrations
{
    /// <inheritdoc />
    public partial class UserFeedIntToLongFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFeeds_UserAccounts_UserAccountId1",
                table: "UserFeeds");

            migrationBuilder.DropIndex(
                name: "IX_UserFeeds_UserAccountId1",
                table: "UserFeeds");

            migrationBuilder.DropColumn(
                name: "UserAccountId1",
                table: "UserFeeds");

            migrationBuilder.CreateIndex(
                name: "IX_UserFeeds_UserAccountId",
                table: "UserFeeds",
                column: "UserAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserFeeds_UserAccounts_UserAccountId",
                table: "UserFeeds",
                column: "UserAccountId",
                principalTable: "UserAccounts",
                principalColumn: "UserAccountId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFeeds_UserAccounts_UserAccountId",
                table: "UserFeeds");

            migrationBuilder.DropIndex(
                name: "IX_UserFeeds_UserAccountId",
                table: "UserFeeds");

            migrationBuilder.AddColumn<long>(
                name: "UserAccountId1",
                table: "UserFeeds",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_UserFeeds_UserAccountId1",
                table: "UserFeeds",
                column: "UserAccountId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserFeeds_UserAccounts_UserAccountId1",
                table: "UserFeeds",
                column: "UserAccountId1",
                principalTable: "UserAccounts",
                principalColumn: "UserAccountId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
