using System.Collections.Generic;

namespace JamesThewProject.Models
{
    public class FeedbackViewModel
    {
        // Danh sách feedback của người dùng
        public IEnumerable<Feedback> Feedbacks { get; set; } = new List<Feedback>();

        // Feedback mới được gửi
        public Feedback NewFeedback { get; set; } = new Feedback();

        // Thuộc tính tìm kiếm và sắp xếp
        public string? SearchString { get; set; }
        public string? SortOrder { get; set; }

        // Thuộc tính phân trang
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }
    }
}
