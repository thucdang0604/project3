using Microsoft.AspNetCore.Mvc;
using JamesThewProject.Data;
using JamesThewProject.Models;
using System;
using System.Linq;

namespace JamesThewProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        public HomeController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }
        // GET: Hiển thị form gửi feedback (không có lịch sử)
        [HttpGet]
        public IActionResult Feedback()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new FeedbackViewModel
            {
                NewFeedback = new Feedback()
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Feedback(FeedbackViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Load danh sách từ khóa nhạy cảm từ database (giả sử model BannedWord có thuộc tính Word)
            var bannedWords = _context.BannedWords.Select(b => b.Word.ToLower()).ToList();
            var lowerContent = model.NewFeedback.Content.ToLower();
            var foundWords = bannedWords.Where(word => lowerContent.Contains(word)).ToList();

            if (foundWords.Any())
            {
                // Thêm lỗi vào ModelState
                ModelState.AddModelError("", "Your feedback contains sensitive words: " + string.Join(", ", foundWords));
                return View(model);
            }

            var feedback = model.NewFeedback;
            feedback.UserId = userId.Value;
            feedback.CreatedAt = DateTime.Now;
            feedback.IsRead = false;

            try
            {
                _context.Feedbacks.Add(feedback);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[EXCEPTION] " + ex.Message);
                return View(model);
            }

            TempData["Message"] = "Feedback sent successfully!";
            return RedirectToAction("Feedback");
        }
        [HttpGet]
        public IActionResult LearnMore()
        {
            // Giả sử LearnMore.cshtml là trang riêng 
            // hiển thị thông tin chi tiết về James Thew
            return View();
        }

        [HttpGet]
        public IActionResult FeedbackHistory(string searchString, string sortOrder, int page = 1)
        {
            int pageSize = 10; // Số feedback hiển thị trên mỗi trang
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var feedbackQuery = _context.Feedbacks.Where(f => f.UserId == userId.Value);

            // Tìm kiếm theo nội dung (không phân biệt chữ hoa, chữ thường)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                feedbackQuery = feedbackQuery.Where(f => f.Content.ToLower().Contains(lowerSearch));
            }

            // Sắp xếp dựa trên lựa chọn của người dùng
            switch (sortOrder)
            {
                case "content_asc":
                    feedbackQuery = feedbackQuery.OrderBy(f => f.Content);
                    break;
                case "content_desc":
                    feedbackQuery = feedbackQuery.OrderByDescending(f => f.Content);
                    break;
                case "date_asc":
                    feedbackQuery = feedbackQuery.OrderBy(f => f.CreatedAt);
                    break;
                case "date_desc":
                    feedbackQuery = feedbackQuery.OrderByDescending(f => f.CreatedAt);
                    break;
                default:
                    feedbackQuery = feedbackQuery.OrderByDescending(f => f.CreatedAt);
                    break;
            }

            // Lấy tổng số feedback để tính số trang
            var totalFeedback = feedbackQuery.Count();
            var totalPages = (int)Math.Ceiling(totalFeedback / (double)pageSize);

            // Lấy dữ liệu cho trang hiện tại
            var feedbacks = feedbackQuery
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToList();

            var model = new FeedbackViewModel
            {
                Feedbacks = feedbacks,
                SearchString = searchString,
                SortOrder = sortOrder,
                PageNumber = page,
                TotalPages = totalPages,
                NewFeedback = new Feedback()
            };

            return View(model);
        }



    }
}
