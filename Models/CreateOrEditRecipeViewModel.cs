using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace JamesThewProject.Models
{
    public class CreateOrEditRecipeViewModel : IValidatableObject
    {
        public int RecipeId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MinLength(3, ErrorMessage = "Title must be at least 3 characters long.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters long.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Instructions are required.")]
        [MinLength(10, ErrorMessage = "Instructions must be at least 10 characters long.")]
        public string Instructions { get; set; } = string.Empty;

        [Required(ErrorMessage = "Servings is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Servings must be a positive number.")]
        public int Servings { get; set; }


        public bool IsPremium { get; set; } = false;
        public bool IsHidden { get; set; } = false;

     
        public IFormFile? ImageFile { get; set; }

        // Ảnh hiện có (dùng cho Edit)
        public string? ExistingImageUrl { get; set; }

        // Danh sách nguyên liệu đã chọn – phải có ít nhất 1 nguyên liệu với IngredientId > 0
        public List<SelectedIngredientViewModel> SelectedIngredients { get; set; } = new List<SelectedIngredientViewModel>();

        // Danh sách nguyên liệu có sẵn (để hiển thị trong dropdown)
        public List<Ingredient> AvailableIngredients { get; set; } = new List<Ingredient>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Kiểm tra phải có ít nhất một nguyên liệu có IngredientId > 0
            if (SelectedIngredients == null || SelectedIngredients.FindAll(si => si.IngredientId > 0).Count == 0)
            {
                yield return new ValidationResult("At least one ingredient must be selected.",
                    new[] { nameof(SelectedIngredients) });
            }

           
        }
    }

    public class SelectedIngredientViewModel
    {
        public int RecipeIngredientId { get; set; }
        [Required(ErrorMessage = "Please select an ingredient.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid ingredient.")]
        public int IngredientId { get; set; }
        public string? Quantity { get; set; }
    }
}
