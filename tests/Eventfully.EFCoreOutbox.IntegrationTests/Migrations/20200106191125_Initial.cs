using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Eventfully.EFCoreOutbox.IntegrationTests.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PriorityDateUtc = table.Column<DateTime>(nullable: false),
                    TryCount = table.Column<int>(nullable: false),
                    Type = table.Column<string>(maxLength: 500, nullable: false),
                    Endpoint = table.Column<string>(maxLength: 500, nullable: true),
                    Status = table.Column<int>(nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessageData",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Data = table.Column<byte[]>(nullable: false),
                    MetaData = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessageData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboxMessageData_OutboxMessages_Id",
                        column: x => x.Id,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriorityDateUtc",
                table: "OutboxMessages",
                columns: new[] { "PriorityDateUtc", "Status" })
                .Annotation("SqlServer:Include", new[] { "TryCount", "Type", "ExpiresAtUtc", "CreatedAtUtc", "Endpoint" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessageData");

            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}
