using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations.Schema;

namespace JamesThewProject.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MinLength(3, ErrorMessage = "Title must be at least 3 characters.")]
        [StringLength(100, ErrorMessage = "The title cannot exceed 100 characters.")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Posting date is required.")]
        [DataType(DataType.Date)]
        public DateTime PostedDate { get; set; }

        [Required(ErrorMessage = "Display status is required.")]
        public bool IsDisplayed { get; set; }

        // Lưu đường dẫn hình ảnh
        public string ImageUrl { get; set; } = "/images/default.png";

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
