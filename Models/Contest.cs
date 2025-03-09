using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JamesThewProject.Models
{
    public class Contest
    {
        [Key]
        public int ContestId { get; set; }

        [Required]
        public string ContestName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation property: danh sách bài dự thi của cuộc thi
        public ICollection<ContestSubmission> Submissions { get; set; } = new List<ContestSubmission>();
    }
}
