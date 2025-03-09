using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JamesThewProject.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeIdToContestSubmission9996599 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecipeId",
                table: "ContestSubmissions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "ContestSubmissions");
        }
    }
}
