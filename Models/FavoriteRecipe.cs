using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JamesThewProject.Models
{
    public class FavoriteRecipe
    {
        [Key]
        public int FavoriteRecipeId { get; set; }

        // Khóa ngoại đến User
        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        // Khóa ngoại đến Recipe
        [Required]
        public int RecipeId { get; set; }
        [ForeignKey("RecipeId")]
        public Recipe? Recipe { get; set; }
    }
}
