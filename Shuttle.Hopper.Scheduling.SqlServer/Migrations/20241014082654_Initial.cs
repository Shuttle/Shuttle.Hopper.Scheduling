using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shuttle.Hopper.Scheduling.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.CreateTable(
                name: "Schedule",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(130)", maxLength: 130, nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(130)", maxLength: 130, nullable: false),
                    NextNotification = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InboxWorkQueueUri = table.Column<string>(type: "nvarchar(130)", maxLength: 130, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedule", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schedule",
                schema: "scheduling",
                table: "Schedule",
                columns: new[] { "Name", "CronExpression", "InboxWorkQueueUri" },
                unique: true,
                filter: "[InboxWorkQueueUri] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schedule",
                schema: "scheduling");
        }
    }
}
