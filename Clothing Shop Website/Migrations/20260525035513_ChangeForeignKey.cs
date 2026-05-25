using Microsoft.EntityFrameworkCore.Migrations;

namespace Clothing_Shop_Website.Migrations
{
    public partial class ChangeForeignKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Products_ProductID",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Products_ProductID",
                table: "OrderDetails");

            migrationBuilder.AlterColumn<int>(
                name: "ProductID",
                table: "OrderDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "SizeID",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ProductID",
                table: "CartItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "SizeID",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_SizeID",
                table: "OrderDetails",
                column: "SizeID");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_SizeID",
                table: "CartItems",
                column: "SizeID");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Products_ProductID",
                table: "CartItems",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_ProductSizes_SizeID",
                table: "CartItems",
                column: "SizeID",
                principalTable: "ProductSizes",
                principalColumn: "SizeID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Products_ProductID",
                table: "OrderDetails",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_ProductSizes_SizeID",
                table: "OrderDetails",
                column: "SizeID",
                principalTable: "ProductSizes",
                principalColumn: "SizeID",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Products_ProductID",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ProductSizes_SizeID",
                table: "CartItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Products_ProductID",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_ProductSizes_SizeID",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_OrderDetails_SizeID",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_SizeID",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "SizeID",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "SizeID",
                table: "CartItems");

            migrationBuilder.AlterColumn<int>(
                name: "ProductID",
                table: "OrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductID",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Products_ProductID",
                table: "CartItems",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Products_ProductID",
                table: "OrderDetails",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
