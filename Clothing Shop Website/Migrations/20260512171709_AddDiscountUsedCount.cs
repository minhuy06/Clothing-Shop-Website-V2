using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clothing_Shop_Website.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountUsedCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsedCount",
                table: "Discounts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsedCount",
                table: "Discounts");
        }
    }
}
