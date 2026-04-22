using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeilleBoisee.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectiviteDashboardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    CommuneInsee = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CommuneName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EncryptedContactEmail = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ParcelleSection = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ParcelleNumero = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsInForest = table.Column<bool>(type: "bit", nullable: true),
                    IsInNatura2000Zone = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CommuneInsee",
                table: "Reports",
                column: "CommuneInsee");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CommuneInsee_Status",
                table: "Reports",
                columns: new[] { "CommuneInsee", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CommuneInsee_SubmittedAt",
                table: "Reports",
                columns: new[] { "CommuneInsee", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_SubmittedAt",
                table: "Reports",
                column: "SubmittedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reports");
        }
    }
}
