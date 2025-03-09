using System;
using System.ComponentModel.DataAnnotations;

namespace JamesThewProject.Models
{
    public class ContestSubmission
    {
        [Key]
        public int SubmissionId { get; set; }

        [Required]
        public int ContestId { get; set; }
        public Contest? Contest { get; set; }

        public int RecipeId { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public string SubmissionContent { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public bool IsWinner { get; set; } = false;
    }
}
