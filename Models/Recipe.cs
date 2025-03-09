using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace JamesThewProject.Models
{
    public class Recipe
    {
        [Key]
        public int RecipeId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public ICollection<FavoriteRecipe> FavoriteRecipes { get; set; } = new List<FavoriteRecipe>();

        public string? Description { get; set; }

        public string? Instructions { get; set; }
        public bool IsPremium { get; set; } = false;

        [Required]
        public int Servings { get; set; }
        public string? ImageUrl { get; set; }

        public bool IsHidden { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Required]
        public int CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

        // Nếu bạn vẫn giữ RecipeComment cho bình luận, có thể có cả collection này
        public ICollection<RecipeComment> RecipeComments { get; set; } = new List<RecipeComment>();

        // Thêm collection cho RecipeRating (tách riêng rating)
        public ICollection<RecipeRating> RecipeRatings { get; set; } = new List<RecipeRating>();

        // Tính trung bình rating dựa trên RecipeRatings; nếu chưa có, mặc định là 5
        [NotMapped]
        public double AverageRating => RecipeRatings.Any() ? RecipeRatings.Average(r => r.Rating) : 5;
    }
}
