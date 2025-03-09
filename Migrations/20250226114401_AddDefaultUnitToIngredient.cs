using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JamesThewProject.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultUnitToIngredient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultUnit",
                table: "Ingredients",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultUnit",
                table: "Ingredients");
        }
    }
}
