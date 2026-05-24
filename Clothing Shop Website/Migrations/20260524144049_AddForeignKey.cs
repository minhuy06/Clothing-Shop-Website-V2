using Microsoft.EntityFrameworkCore.Migrations;

namespace Clothing_Shop_Website.Migrations
{
    public partial class AddForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductID",
                table: "Advertisements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_ProductID",
                table: "Advertisements",
                column: "ProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_Advertisements_Products_ProductID",
                table: "Advertisements",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Advertisements_Products_ProductID",
                table: "Advertisements");

            migrationBuilder.DropIndex(
                name: "IX_Advertisements_ProductID",
                table: "Advertisements");

            migrationBuilder.DropColumn(
                name: "ProductID",
                table: "Advertisements");
        }
    }
}
