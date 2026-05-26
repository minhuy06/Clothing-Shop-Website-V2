using Microsoft.EntityFrameworkCore.Migrations;

namespace Clothing_Shop_Website.Migrations
{
    public partial class AddAdvertisementSlotIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlotIndex",
                table: "Advertisements",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlotIndex",
                table: "Advertisements");
        }
    }
}
