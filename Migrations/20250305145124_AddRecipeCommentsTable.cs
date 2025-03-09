using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JamesThewProject.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeCommentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeComment_Recipes_RecipeId",
                table: "RecipeComment");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeComment_Users_UserId",
                table: "RecipeComment");

            migrationBuilder.DropIndex(
                name: "IX_RecipeRatings_RecipeId",
                table: "RecipeRatings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecipeComment",
                table: "RecipeComment");

            migrationBuilder.RenameTable(
                name: "RecipeComment",
                newName: "RecipeComments");

            migrationBuilder.RenameIndex(
                name: "IX_RecipeComment_UserId",
                table: "RecipeComments",
                newName: "IX_RecipeComments_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RecipeComment_RecipeId",
                table: "RecipeComments",
                newName: "IX_RecipeComments_RecipeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecipeComments",
                table: "RecipeComments",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeRatings_RecipeId_UserId",
                table: "RecipeRatings",
                columns: new[] { "RecipeId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeComments_Recipes_RecipeId",
                table: "RecipeComments",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "RecipeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeComments_Users_UserId",
                table: "RecipeComments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeComments_Recipes_RecipeId",
                table: "RecipeComments");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeComments_Users_UserId",
                table: "RecipeComments");

            migrationBuilder.DropIndex(
                name: "IX_RecipeRatings_RecipeId_UserId",
                table: "RecipeRatings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecipeComments",
                table: "RecipeComments");

            migrationBuilder.RenameTable(
                name: "RecipeComments",
                newName: "RecipeComment");

            migrationBuilder.RenameIndex(
                name: "IX_RecipeComments_UserId",
                table: "RecipeComment",
                newName: "IX_RecipeComment_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RecipeComments_RecipeId",
                table: "RecipeComment",
                newName: "IX_RecipeComment_RecipeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecipeComment",
                table: "RecipeComment",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeRatings_RecipeId",
                table: "RecipeRatings",
                column: "RecipeId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeComment_Recipes_RecipeId",
                table: "RecipeComment",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "RecipeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeComment_Users_UserId",
                table: "RecipeComment",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
