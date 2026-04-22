using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeilleBoisee.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoData",
                table: "Reports",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoMimeType",
                table: "Reports",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "PhotoMimeType",
                table: "Reports");
        }
    }
}
