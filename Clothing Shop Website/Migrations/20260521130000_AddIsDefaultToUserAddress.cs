using Microsoft.EntityFrameworkCore.Migrations;

namespace Clothing_Shop_Website.Migrations
{
    public partial class AddIsDefaultToUserAddress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "UserAddresses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "UserAddresses");
        }
    }
}
