namespace JamesThewProject.Models
{
    public class RecipeRatingViewModel
    {
        public int RecipeId { get; set; }
        public string RecipeTitle { get; set; } = string.Empty;
        // Giá trị Rating của người dùng (mặc định là 5 nếu chưa chấm)
        public int Rating { get; set; } = 5;
    }
}
