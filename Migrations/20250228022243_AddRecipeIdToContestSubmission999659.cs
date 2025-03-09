using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JamesThewProject.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeIdToContestSubmission999659 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContestSubmissions_Contests_ContestId",
                table: "ContestSubmissions");

            migrationBuilder.AddForeignKey(
                name: "FK_ContestSubmissions_Contests_ContestId",
                table: "ContestSubmissions",
                column: "ContestId",
                principalTable: "Contests",
                principalColumn: "ContestId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContestSubmissions_Contests_ContestId",
                table: "ContestSubmissions");

            migrationBuilder.AddForeignKey(
                name: "FK_ContestSubmissions_Contests_ContestId",
                table: "ContestSubmissions",
                column: "ContestId",
                principalTable: "Contests",
                principalColumn: "ContestId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
