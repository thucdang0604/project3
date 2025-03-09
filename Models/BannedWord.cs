using System.ComponentModel.DataAnnotations;

namespace JamesThewProject.Models
{
    public class BannedWord
    {
        [Key]
        public int BannedWordId { get; set; }

        [Required(ErrorMessage = "Word is required")]
        [StringLength(100)]
        public string Word { get; set; } = string.Empty;
    }
}
