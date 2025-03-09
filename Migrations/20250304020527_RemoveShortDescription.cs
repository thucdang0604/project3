using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JamesThewProject.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShortDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "Recipes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "Recipes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
