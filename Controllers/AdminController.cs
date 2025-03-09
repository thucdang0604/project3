using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using JamesThewProject.Data;
using JamesThewProject.Models;
using System.Linq;
using System;
using System.Security.Cryptography;
using System.Text;
using JamesThewProject.Services;

namespace JamesThewProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        public AdminController(AppDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }
        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // Giả sử bạn có các DbSet: Users, Recipes, Contests, Feedbacks trong DbContext

            int totalUsers = await _context.Users.CountAsync();
            int totalRecipes = await _context.Recipes.CountAsync();
            int activeContests = await _context.Contests.CountAsync(c => c.IsActive);
            int newFeedback = await _context.Feedbacks.CountAsync(f => !f.IsRead); // Feedback mới là feedback chưa đọc

            var model = new DashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalRecipes = totalRecipes,
                ActiveContests = activeContests,
                NewFeedback = newFeedback
            };

            return View(model);
        }
        [HttpGet]
        public IActionResult ManagePlans(string searchString, string sortOrder)
        {
            // Kiểm tra role Admin
            string role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (role != "Admin" && role != "Manager")
            {
                return RedirectToAction("Index", "Home");
            }

            // Tạo truy vấn trên DbSet SubscriptionPlans
            var plansQuery = _context.SubscriptionPlans.AsQueryable();

            // Lọc theo từ khóa: tìm kiếm theo PlanName hoặc Description (không phân biệt chữ hoa/chữ thường)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                plansQuery = plansQuery.Where(p => p.PlanName.ToLower().Contains(lowerSearch) ||
                                                    (p.Description != null && p.Description.ToLower().Contains(lowerSearch)));
            }

            // Sắp xếp theo lựa chọn của người dùng
            switch (sortOrder)
            {
                case "name_asc":
                    plansQuery = plansQuery.OrderBy(p => p.PlanName);
                    break;
                case "name_desc":
                    plansQuery = plansQuery.OrderByDescending(p => p.PlanName);
                    break;
                case "price_asc":
                    plansQuery = plansQuery.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    plansQuery = plansQuery.OrderByDescending(p => p.Price);
                    break;
                case "duration_asc":
                    plansQuery = plansQuery.OrderBy(p => p.DurationInDays);
                    break;
                case "duration_desc":
                    plansQuery = plansQuery.OrderByDescending(p => p.DurationInDays);
                    break;
                default:
                    // Sắp xếp mặc định theo ngày tạo giảm dần
                    plansQuery = plansQuery.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            // Lưu lại giá trị tìm kiếm và sắp xếp vào ViewBag để hiển thị lại trong view
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            var plans = plansQuery.ToList();
            return View(plans);
        }

        // ✅ Hiển thị form thêm gói mới
        [HttpGet]
        public IActionResult CreatePlan()
        {
            var plan = new SubscriptionPlan
            {
                PlanName = "Test Plan",      // Rỗng mặc định
                Price = 9.99m,
                DurationInDays = 30,
                IsActive = true
            };
            return View(plan);
        }

        [HttpPost]
        public IActionResult CreatePlan(SubscriptionPlan plan)
        {
            Console.WriteLine("[DEBUG] CreatePlan POST called");
            Console.WriteLine($"PlanName='{plan.PlanName}', Description='{plan.Description}', Price={plan.Price}, DurationInDays={plan.DurationInDays}, IsActive={plan.IsActive}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("[ERROR] ModelState invalid.");
                foreach (var key in ModelState.Keys)
                {
                    foreach (var err in ModelState[key].Errors)
                    {
                        Console.WriteLine($"[ERROR] {key}: {err.ErrorMessage}");
                    }
                }
                return View(plan);
            }

            try
            {
                plan.CreatedAt = DateTime.Now;
                plan.UpdatedAt = DateTime.Now;
                _context.SubscriptionPlans.Add(plan);
                _context.SaveChanges();
                Console.WriteLine("[SUCCESS] Plan created successfully.");
                return RedirectToAction("ManagePlans");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[EXCEPTION] " + ex.Message);
                return View(plan);
            }
        }



        [HttpGet]
        public IActionResult EditPlan(int id)
        {
            // Debug log: kiểm tra id nhận được
            Console.WriteLine($"[DEBUG] EditPlan GET called with ID: {id}");

            // Lấy record theo PlanId
            var plan = _context.SubscriptionPlans.FirstOrDefault(p => p.PlanId == id);
            if (plan == null)
            {
                Console.WriteLine($"[ERROR] No SubscriptionPlan found with ID: {id}");
                return NotFound("Plan not found");
            }

            Console.WriteLine($"[DEBUG] Loaded Plan: {plan.PlanName}, Price: {plan.Price}, Duration: {plan.DurationInDays}");
            return View(plan);
        }
        public IActionResult ManageFeedback(string searchString, string sortOrder)
        {
            // Tạo truy vấn trên DbSet Feedbacks kèm thông tin User
            var feedbacksQuery = _context.Feedbacks.Include(f => f.User).AsQueryable();

            // Lọc theo từ khóa (tìm kiếm trong Email của User và nội dung Feedback)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                feedbacksQuery = feedbacksQuery.Where(f =>
                    (f.User != null && f.User.Email.ToLower().Contains(lowerSearch)) ||
                    f.Content.ToLower().Contains(lowerSearch));
            }

            // Sắp xếp theo lựa chọn của người dùng
            switch (sortOrder)
            {
                case "email_asc":
                    feedbacksQuery = feedbacksQuery.OrderBy(f => f.User.Email);
                    break;
                case "email_desc":
                    feedbacksQuery = feedbacksQuery.OrderByDescending(f => f.User.Email);
                    break;
                case "date_asc":
                    feedbacksQuery = feedbacksQuery.OrderBy(f => f.CreatedAt);
                    break;
                case "date_desc":
                    feedbacksQuery = feedbacksQuery.OrderByDescending(f => f.CreatedAt);
                    break;
                case "status_asc":
                    // Ưu tiên những feedback chưa đọc
                    feedbacksQuery = feedbacksQuery.OrderBy(f => f.IsRead);
                    break;
                case "status_desc":
                    feedbacksQuery = feedbacksQuery.OrderByDescending(f => f.IsRead);
                    break;
                default:
                    // Mặc định: sắp xếp theo ngày gửi giảm dần
                    feedbacksQuery = feedbacksQuery.OrderByDescending(f => f.CreatedAt);
                    break;
            }

            // Lưu các giá trị tìm kiếm, sắp xếp để hiển thị lại trong view
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            var feedbacks = feedbacksQuery.ToList();
            return View(feedbacks);
        }


        // GET: /Admin/ReplyFeedback/{id} – Hiển thị form trả lời feedback
        [HttpGet]
        public IActionResult ReplyFeedback(int id)
        {
            var feedback = _context.Feedbacks.Include(f => f.User).FirstOrDefault(f => f.FeedbackId == id);
            if (feedback == null)
            {
                return NotFound("Feedback not found");
            }
            return View(feedback);
        }

        [HttpPost]
        public IActionResult ReplyFeedback(Feedback model)
        {
            // Loại bỏ lỗi ModelState đối với Content vì admin không nhập lại
            ModelState.Remove("Content");

            Console.WriteLine("[DEBUG] ReplyFeedback POST called");
            Console.WriteLine($"FeedbackId: {model.FeedbackId}, New Response: '{model.Response}'");

            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    foreach (var error in ModelState[key].Errors)
                    {
                        Console.WriteLine($"[ERROR] {key}: {error.ErrorMessage}");
                    }
                }
                return View(model);
            }

            var feedback = _context.Feedbacks.FirstOrDefault(f => f.FeedbackId == model.FeedbackId);
            if (feedback == null)
            {
                return NotFound("Feedback not found");
            }

            // Cập nhật định dạng ngày giờ theo dd/MM/yyyy HH:mm
            string formattedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // Nếu có phản hồi cũ, nối phản hồi mới vào phản hồi cũ
            if (!string.IsNullOrWhiteSpace(feedback.Response))
            {
                feedback.Response += "\n\n[" + formattedDate + "]: " + model.Response;
            }
            else
            {
                feedback.Response = "[" + formattedDate + "]: " + model.Response;
            }

            feedback.IsRead = true;
            feedback.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            Console.WriteLine("[SUCCESS] Feedback updated with response.");
            return RedirectToAction("ManageFeedback");
        }

        [HttpPost]
        public IActionResult EditPlan(SubscriptionPlan model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingPlan = _context.SubscriptionPlans.FirstOrDefault(p => p.PlanId == model.PlanId);
            if (existingPlan == null)
            {
                return NotFound("Plan not found");
            }

            existingPlan.IsActive = model.IsActive;
            existingPlan.UpdatedAt = DateTime.Now;

            _context.SubscriptionPlans.Update(existingPlan);
            _context.SaveChanges();

            return RedirectToAction("ManagePlans");
        }
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Forbid();
            return View();
        }  // POST: /Admin/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Forbid();

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.Role == "Admin");
            if (adminUser == null)
                return RedirectToAction("Login", "Account");

            if (adminUser.PasswordHash != HashPassword(currentPassword))
            {
                ViewBag.ErrorMessage = "Current password is incorrect.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "New password and confirm password do not match.";
                return View();
            }

            adminUser.PasswordHash = HashPassword(newPassword);
            adminUser.UpdatedAt = DateTime.Now;
            _context.Users.Update(adminUser);
            await _context.SaveChangesAsync();

            ViewBag.Message = "Password changed successfully.";
            return View();
        }
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // Sinh mật khẩu ngẫu nhiên (đánh dấu static, không phụ thuộc instance)
        public static string GenerateRandomPassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            using var rng = RandomNumberGenerator.Create();
            byte[] uintBuffer = new byte[sizeof(uint)];
            while (length-- > 0)
            {
                rng.GetBytes(uintBuffer);
                uint num = BitConverter.ToUInt32(uintBuffer, 0);
                res.Append(validChars[(int)(num % (uint)validChars.Length)]);
            }
            return res.ToString();
        }
        public async Task<IActionResult> ManageUsers(string searchString, string sortOrder)
        {
            // Loại bỏ user có Role = "Admin"
            var users = _context.Users
                                .Where(u => u.Role != "Admin")  // <-- THÊM ĐIỀU KIỆN Ở ĐÂY
                                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                users = users.Where(u => u.Email.ToLower().Contains(lowerSearch)
                                         || u.FullName.ToLower().Contains(lowerSearch));
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "email_asc":
                    users = users.OrderBy(u => u.Email);
                    break;
                case "email_desc":
                    users = users.OrderByDescending(u => u.Email);
                    break;
                case "name_asc":
                    users = users.OrderBy(u => u.FullName);
                    break;
                case "name_desc":
                    users = users.OrderByDescending(u => u.FullName);
                    break;
                default:
                    users = users.OrderBy(u => u.UserId);
                    break;
            }

            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            return View(await users.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Trả về View EditUser.cshtml cùng với model user
            return View(existingUser);
        }

        // POST: /Admin/EditUser/4
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, [Bind("UserId,IsActive")] User user)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return NotFound();
            }

            // Kiểm tra tính nhất quán của id
            if (id != user.UserId)
            {
                return BadRequest("User ID mismatch");
            }

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            existingUser.IsActive = user.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(ManageUsers));
        }
        // GET: /Admin/CreateManager
        [HttpGet]
        public IActionResult CreateManager()
        {
            // Chỉ hiển thị form
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateManager(JamesThewProject.Models.User model)
        {
            // Loại bỏ lỗi kiểm tra PasswordHash vì nó sẽ được tạo tự động
            ModelState.Remove("PasswordHash");

            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"[MODEL ERROR] {state.Key}: {error.ErrorMessage}");
                    }
                }
                return View(model);
            }

            // Kiểm tra xem email đã tồn tại chưa
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            // Sinh mật khẩu ngẫu nhiên
            var randomPassword = GenerateRandomPassword(8);
            model.PasswordHash = HashPassword(randomPassword);

            // Gán các giá trị mặc định cho Manager
            model.Role = "Manager";
            model.IsActive = true;
            model.IsActive = true;
            model.IsEmailConfirmed = true;
            model.MembershipType = "Free";
            model.IsProfileComplete = true;
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            // Thêm user mới vào DbContext và lưu vào database
            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            // Gửi email chứa mật khẩu ngẫu nhiên cho Manager
            var subject = "Manager account created";
            var body = $"<p>Your manager account has been created successfully.</p>" +
                       $"<p>Your password is: <strong>{randomPassword}</strong></p>" +
                       $"<p>Please change your password after logging in.</p>";
            await _emailSender.SendEmailAsync(model.Email, subject, body);

            // Hiển thị thông báo thành công (cho Admin biết mật khẩu mới)
            TempData["SuccessMessage"] = $"Manager created successfully. Email: {model.Email}, Password: {randomPassword}";
            return RedirectToAction("ManageUsers");
        }

        [HttpGet]
        public async Task<IActionResult> ManageComments(string searchString, string sortOrder)
        {
            // Kiểm tra role Admin (nếu có)
            string role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (role != "Admin" && role != "Manager")
            {
                return RedirectToAction("Index", "Home");
            }

            // Tạo truy vấn kèm Include User và Recipe
            var commentsQuery = _context.RecipeComments
                .Include(c => c.User)
                .Include(c => c.Recipe)
                .AsQueryable();

            // Lọc theo từ khóa (tìm trong CommentText, User.Email, Recipe.Title)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                commentsQuery = commentsQuery.Where(c =>
                    c.CommentText.ToLower().Contains(lowerSearch)
                    || (c.User != null && c.User.Email.ToLower().Contains(lowerSearch))
                    || (c.Recipe != null && c.Recipe.Title.ToLower().Contains(lowerSearch))
                );
            }

            // Sắp xếp
            // - date_asc: cũ -> mới
            // - date_desc: mới -> cũ
            // - visible_asc: ẩn -> hiện
            // - visible_desc: hiện -> ẩn
            switch (sortOrder)
            {
                case "date_asc":
                    commentsQuery = commentsQuery.OrderBy(c => c.CreatedAt);
                    break;
                case "date_desc":
                    commentsQuery = commentsQuery.OrderByDescending(c => c.CreatedAt);
                    break;
                case "visible_asc":
                    // Sắp xếp những comment ẩn (IsVisible=false) lên trước
                    commentsQuery = commentsQuery.OrderBy(c => c.IsVisible);
                    break;
                case "visible_desc":
                    commentsQuery = commentsQuery.OrderByDescending(c => c.IsVisible);
                    break;
                default:
                    // Mặc định: Mới nhất lên trước
                    commentsQuery = commentsQuery.OrderByDescending(c => c.CreatedAt);
                    break;
            }

            // Lưu giá trị để hiển thị lại trên View
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            // Lấy kết quả
            var comments = await commentsQuery.ToListAsync();
            return View(comments);
        }

        [HttpGet]
        public async Task<IActionResult> EditComment(int? id)
        {
            // Kiểm tra id
            if (!id.HasValue)
            {
                return BadRequest("ID is required.");
            }

            // Tìm bình luận theo ID
            var comment = await _context.RecipeComments
                .Include(c => c.User)
                .Include(c => c.Recipe)
                .FirstOrDefaultAsync(c => c.CommentId == id.Value);

            if (comment == null)
            {
                return NotFound("Comment not found");
            }

            // Trả về view, truyền comment làm model
            return View(comment);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(int id, string CommentText, bool IsVisible)
        {
            // Tìm bình luận theo id
            var comment = await _context.RecipeComments.FindAsync(id);
            if (comment == null)
            {
                return NotFound("Comment not found");
            }

            // Cập nhật trường
            comment.CommentText = CommentText;
            comment.IsVisible = IsVisible;
            // Nếu có cột UpdatedAt thì cập nhật:
            // comment.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Quay lại trang ManageComments
            return RedirectToAction(nameof(ManageComments));
        }


        // ✅ Xóa gói nâng cấp
        [HttpPost]
        public IActionResult DeletePlan(int id)
        {
            var plan = _context.SubscriptionPlans.Find(id);
            if (plan != null)
            {
                _context.SubscriptionPlans.Remove(plan);
                _context.SaveChanges();
            }
            return RedirectToAction("ManagePlans");
        }
    }
}
