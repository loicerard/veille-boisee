using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeilleBoisee.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmitterUserIdToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubmitterUserId",
                table: "Reports",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_SubmitterUserId",
                table: "Reports",
                column: "SubmitterUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_SubmitterUserId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "SubmitterUserId",
                table: "Reports");
        }
    }
}
