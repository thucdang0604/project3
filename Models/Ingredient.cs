using System.ComponentModel.DataAnnotations;

namespace JamesThewProject.Models
{
    public class Ingredient
    {
        [Key]
        public int IngredientId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // Đơn vị mặc định (VD: "gam", "con", "trái", ...)
        public string? DefaultUnit { get; set; }
    }
}
