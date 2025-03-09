using System;
using System.ComponentModel.DataAnnotations;

namespace JamesThewProject.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public ICollection<FavoriteRecipe> FavoriteRecipes { get; set; } = new List<FavoriteRecipe>();
        public ICollection<RecipeRating> RecipeRatings { get; set; } = new List<RecipeRating>();
        public ICollection<RecipeComment> RecipeComments { get; set; } = new List<RecipeComment>();
        [Required]
        public string Role { get; set; } = "User";

        public bool IsActive { get; set; } = true;
        public bool IsProfileComplete { get; set; } = false;

        // Các trường mới hỗ trợ xác thực email:
        public bool IsEmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiry { get; set; }

        // Các thuộc tính khác
        [Required]
        public string MembershipType { get; set; } = "Free";
        public DateTime? MembershipExpiry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
