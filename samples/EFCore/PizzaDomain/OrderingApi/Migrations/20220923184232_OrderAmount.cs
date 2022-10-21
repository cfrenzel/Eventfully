using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.Api.Migrations
{
    public partial class OrderAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Orders",
                type: "decimal(11,2)",
                precision: 11,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Orders");
        }
    }
}
