using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using JamesThewProject.Data;
using JamesThewProject.Models;
using JamesThewProject.Services;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace JamesThewProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailSender _emailSender;

        public AccountController(AppDbContext context, IWebHostEnvironment environment, IEmailSender emailSender)
        {
            _context = context;
            _environment = environment;
            _emailSender = emailSender;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                string userRole = HttpContext.Session.GetString("UserRole") ?? "User";
                return userRole == "Admin"
                    ? RedirectToAction("Dashboard", "Admin")
                    : RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Nếu session đã tồn tại, chuyển hướng
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                string userRole = HttpContext.Session.GetString("UserRole") ?? "User";
                return (userRole == "Admin" || userRole == "Manager")
                    ? RedirectToAction("Dashboard", "Admin")
                    : RedirectToAction("Index", "Home");
            }

            string hashedPassword = HashPassword(password);
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == hashedPassword);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View();
            }
            if (!user.IsActive)
            {
                ViewBag.ErrorMessage = "Your account has been deactivated.";
                return View();
            }
            if (!user.IsEmailConfirmed)
            {
                ViewBag.ErrorMessage = "Please confirm your email before logging in.";
                return View();
            }

            // Kiểm tra nếu hồ sơ chưa hoàn thiện
            if (!user.IsProfileComplete)
            {
                // Lưu thông tin cần thiết vào session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                // Chuyển hướng đến trang hoàn thiện hồ sơ
                return RedirectToAction("CompleteProfile", "Account");
            }

            // Nếu đã hoàn thiện, lưu thông tin vào session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("MembershipType", user.MembershipType ?? "Free");

            return (user.Role == "Admin" || user.Role == "Manager")
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Index", "Home");
        }


        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.ErrorMessage = "Email already exists.";
                return View();
            }
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View();
            }

            // Tạo user mới với IsEmailConfirmed = false
            var newUser = new User
            {
                Email = email,
                PasswordHash = HashPassword(password),
                Role = "User",
                IsProfileComplete = false,
                IsEmailConfirmed = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            // Sinh token xác nhận email và lưu trong DB
            var token = Guid.NewGuid().ToString();
            newUser.EmailConfirmationToken = token;
            newUser.EmailConfirmationTokenExpiry = DateTime.Now.AddHours(24);
            _context.SaveChanges();

            // Tạo link xác nhận email
            var confirmUrl = Url.Action("ConfirmEmail", "Account", new { userId = newUser.UserId, token = token }, Request.Scheme);
            string subject = "Please confirm your email";
            string body = $"<p>Please confirm your email by clicking <a href='{confirmUrl}'>here</a>.</p>";

            await _emailSender.SendEmailAsync(newUser.Email, subject, body);

            ViewBag.Message = "Registration successful! Please check your email to confirm your account.";
            return View("RegisterSuccess");
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        public IActionResult ConfirmEmail(int userId, string token)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null || user.EmailConfirmationToken != token)
            {
                return BadRequest("Invalid confirmation link.");
            }
            if (user.EmailConfirmationTokenExpiry < DateTime.Now)
            {
                return BadRequest("Confirmation link expired.");
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpiry = null;
            _context.SaveChanges();

            ViewBag.Message = "Email confirmed successfully! You can now log in.";
            return View("ConfirmEmailSuccess");
        }

        // ✅ Hiển thị trang nâng cấp tài khoản
        [HttpGet]
        public IActionResult UpgradeMembership()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Nếu đã là Premium, chuyển hướng đến trang công thức cao cấp
            if (user.MembershipType == "Premium" && user.MembershipExpiry.HasValue && user.MembershipExpiry.Value > DateTime.Now)
            {
                return RedirectToAction("PremiumRecipes", "Recipes");
            }

            // Load danh sách gói nâng cấp từ database
            var plans = _context.SubscriptionPlans.Where(p => p.IsActive).ToList();
            ViewBag.Plans = plans;

            return View(user);
        }
      [HttpPost]
public IActionResult UpgradeMembership(int planId)
{
    int? userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null)
    {
        return RedirectToAction("Login");
    }

    var user = _context.Users.Find(userId);
    if (user == null)
    {
        return RedirectToAction("Login");
    }

    // Nếu đã là Premium và chưa hết hạn
    if (user.MembershipType == "Premium" && user.MembershipExpiry.HasValue && user.MembershipExpiry.Value > DateTime.Now)
    {
        TempData["ErrorMessage"] = $"Your Premium membership is active until {user.MembershipExpiry.Value:yyyy-MM-dd}.";
        return RedirectToAction("Profile", "Account");
    }

    // Lấy gói nâng cấp
    var plan = _context.SubscriptionPlans.FirstOrDefault(p => p.PlanId == planId && p.IsActive);
    if (plan == null)
    {
        TempData["ErrorMessage"] = "Selected plan is not available.";
        return RedirectToAction("UpgradeMembership");
    }

    // Tính thời hạn gói
    DateTime startDate = DateTime.Now;
    DateTime endDate = startDate.AddDays(plan.DurationInDays);

    // Cập nhật thông tin Membership trong bảng Users
    user.MembershipType = "Premium";
    user.MembershipExpiry = endDate;
    user.UpdatedAt = DateTime.Now;
    _context.Users.Update(user);
    _context.SaveChanges();

    // Tạo Subscription (lịch sử nâng cấp)
    var subscription = new Subscription
    {
        UserId = user.UserId,
        PlanId = plan.PlanId,
        StartDate = startDate,
        EndDate = endDate,
        PaymentStatus = "Paid", // hoặc "Pending" tùy logic
        CreatedAt = DateTime.Now
    };
    _context.Subscriptions.Add(subscription);
    _context.SaveChanges();

    // Cập nhật session nếu cần
    HttpContext.Session.SetString("MembershipType", "Premium");

    // Gửi email thông báo
    string subject = "Your Premium Membership is Activated!";
    string body = $@"<p>Dear {(user.FullName ?? user.Email)},</p>
                     <p>Your account has been upgraded to Premium.</p>
                     <p><strong>Plan:</strong> {plan.PlanName}</p>
                     <p><strong>Price:</strong> ${plan.Price}</p>
                     <p><strong>Duration:</strong> {plan.DurationInDays} days</p>
                     <p><strong>Valid until:</strong> {endDate:yyyy-MM-dd}</p>
                     <p>Thank you for choosing our service.</p>";
    _emailSender.SendEmailAsync(user.Email, subject, body).Wait();

    // Chuyển hướng đến trang UpgradeSuccess
    return RedirectToAction("UpgradeSuccess", new { planId = plan.PlanId });
}
        // GET: /Account/Payment?planId=...
        [HttpGet]
        public IActionResult Payment(int planId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login");

            var plan = _context.SubscriptionPlans.FirstOrDefault(p => p.PlanId == planId && p.IsActive);
            if (plan == null)
            {
                TempData["ErrorMessage"] = "Selected plan is not available.";
                return RedirectToAction("SubscriptionHistory", "Account");
            }

            // Tạo PaymentViewModel (bạn cần tạo model này)
            var model = new PaymentViewModel();
            ViewBag.Plan = plan; // truyền thông tin gói sang view nếu cần hiển thị

            return View(model); // Mặc định sẽ trả về view Payment.cshtml
        }

        // Ví dụ trong action POST Payment (hoặc UpgradeMembership)
        [HttpPost]
        public IActionResult Payment(PaymentViewModel paymentModel, int planId)
        {
            if (!ModelState.IsValid)
            {
                var planX = _context.SubscriptionPlans.FirstOrDefault(p => p.PlanId == planId && p.IsActive);
                ViewBag.Plan = planX;
                return View("Payment", paymentModel);
            }

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            var plan = _context.SubscriptionPlans.FirstOrDefault(p => p.PlanId == planId && p.IsActive);
            if (plan == null)
            {
                TempData["ErrorMessage"] = "Selected plan is not available.";
                return RedirectToAction("SubscriptionHistory");
            }

            // Giả lập xử lý thanh toán
            // -------------
            // Tính logic gia hạn
            DateTime now = DateTime.Now;
            DateTime startDate;
            DateTime endDate;

            // Nếu user đang có Premium và chưa hết hạn -> cộng thêm
            if (user.MembershipType == "Premium" && user.MembershipExpiry.HasValue && user.MembershipExpiry.Value > now)
            {
                // startDate = ngày hết hạn cũ
                startDate = user.MembershipExpiry.Value;
                // endDate = ngày hết hạn cũ + số ngày của gói mới
                endDate = user.MembershipExpiry.Value.AddDays(plan.DurationInDays);
            }
            else
            {
                // Nếu user chưa có Premium hoặc đã hết hạn, tính từ hôm nay
                startDate = now;
                endDate = now.AddDays(plan.DurationInDays);
            }

            // Cập nhật user
            user.MembershipType = "Premium";
            user.MembershipExpiry = endDate;
            user.UpdatedAt = now;
            _context.Users.Update(user);

            // Tạo Subscription
            var subscription = new Subscription
            {
                UserId = user.UserId,
                PlanId = plan.PlanId,
                StartDate = startDate,
                EndDate = endDate,
                PaymentStatus = "Paid",
                CreatedAt = now
            };
            _context.Subscriptions.Add(subscription);
            _context.SaveChanges();

            // Cập nhật session
            HttpContext.Session.SetString("MembershipType", "Premium");

            // Gửi email thông báo
            string subject = "Your Premium Membership is Extended!";
            string body = $@"<p>Dear {(user.FullName ?? user.Email)},</p>
                     <p>Your premium membership has been extended.</p>
                     <p><strong>Plan:</strong> {plan.PlanName}</p>
                     <p><strong>Price:</strong> ${plan.Price}</p>
                     <p><strong>Duration:</strong> {plan.DurationInDays} days</p>
                     <p><strong>New expiry date:</strong> {endDate:yyyy-MM-dd}</p>
                     <p>Thank you for choosing our service.</p>";
            _emailSender.SendEmailAsync(user.Email, subject, body).Wait();

            // Chuyển hướng
            TempData["SuccessMessage"] = "Your membership has been extended successfully.";
            return RedirectToAction("UpgradeSuccess", new { planId = plan.PlanId });
        }



        // GET: /Account/SubscriptionHistory
        [HttpGet]
        public async Task<IActionResult> SubscriptionHistory()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            // Lấy lịch sử nâng cấp của user
            var subscriptions = await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId)
                .ToListAsync();

            // Lấy danh sách các gói nâng cấp hiện có
            var plans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .ToListAsync();

            var viewModel = new ManageSubscriptionsViewModel
            {
                Subscriptions = subscriptions,
                Plans = plans
            };

            return View(viewModel); // View: SubscriptionHistory.cshtml
        }



        [HttpGet]
        public IActionResult UpgradeSuccess(int planId)
        {
            // Lấy gói membership vừa nâng cấp để hiển thị thông tin
            var plan = _context.SubscriptionPlans.FirstOrDefault(p => p.PlanId == planId);
            if (plan == null)
            {
                TempData["ErrorMessage"] = "Plan not found.";
                return RedirectToAction("UpgradeMembership");
            }
            // Bạn cũng có thể lấy thông tin user nếu cần
            return View(plan);
        }


        // GET: /Account/Profile
        [HttpGet]
        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // GET: /Account/CompleteProfile
        [HttpGet]
        public IActionResult CompleteProfile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // POST: /Account/CompleteProfile
        [HttpPost]
        public IActionResult CompleteProfile(User model, IFormFile? avatarFile)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Load danh sách từ khóa nhạy cảm từ database
            var bannedWords = _context.BannedWords.Select(b => b.Word.ToLower()).ToList();
            var errors = new System.Collections.Generic.List<string>();

            // Kiểm tra FullName
            if (!string.IsNullOrEmpty(model.FullName) && bannedWords.Any(word => model.FullName.ToLower().Contains(word)))
            {
                errors.Add("Full name contains banned words.");
            }

            // Kiểm tra Address (nếu cần)
            if (!string.IsNullOrEmpty(model.Address) && bannedWords.Any(word => model.Address.ToLower().Contains(word)))
            {
                errors.Add("Address contains banned words.");
            }

            // Nếu có lỗi, thêm vào ModelState và trả về view để người dùng chỉnh sửa
            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
                return View(user);
            }

            // Cập nhật thông tin hồ sơ
            user.FullName = model.FullName;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.Now;

            // Xử lý upload avatar nếu có
            if (avatarFile != null && avatarFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(avatarFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    avatarFile.CopyTo(fileStream);
                }
                user.AvatarUrl = "/uploads/" + uniqueFileName;
            }

            // Đánh dấu hồ sơ đã hoàn thiện
            user.IsProfileComplete = true;
            _context.Users.Update(user);
            _context.SaveChanges();

            // Sau khi hoàn thiện hồ sơ, chuyển hướng về trang Profile
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            // Lấy userId từ session
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Tìm user theo userId
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Truyền model user sang view
            return View(user);
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string phoneNumber)
        {
            // Kiểm tra user theo email và số điện thoại
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.PhoneNumber == phoneNumber);

            if (user == null)
            {
                ViewBag.ErrorMessage = "Email or phone number is invalid.";
                return View();
            }

            // Kiểm tra nếu user có role là Admin
            if (user.Role == "Admin")
            {
                // Không cho phép Admin reset password bằng trang này
                ViewBag.ErrorMessage = "Admin accounts cannot reset password here.";
                return View();
            }

            // Sinh mật khẩu ngẫu nhiên (ví dụ độ dài 8 ký tự)
            string newPassword = GenerateRandomPassword(8);
            user.PasswordHash = HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Gửi mật khẩu mới qua email
            string subject = "Your new password";
            string body = $"<p>Your new password is: <strong>{newPassword}</strong></p>" +
                          "<p>Please change your password after logging in.</p>";
            await _emailSender.SendEmailAsync(user.Email, subject, body);

            ViewBag.Message = "A new password has been sent to your email.";
            return View("ResetPasswordSuccess");
        }

        // Sinh mật khẩu ngẫu nhiên
        private string GenerateRandomPassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];
                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(validChars[(int)(num % (uint)validChars.Length)]);
                }
            }
            return res.ToString();
        }
        // GET: /Account/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            // Lấy email của user từ session
            string userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            // Tìm user theo email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra mật khẩu hiện tại có khớp không
            if (user.PasswordHash != HashPassword(currentPassword))
            {
                ViewBag.ErrorMessage = "Current password is incorrect.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.ErrorMessage = "New password and confirm password do not match.";
                return View();
            }

            // Cập nhật mật khẩu mới
            user.PasswordHash = HashPassword(newPassword);
            user.UpdatedAt = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            ViewBag.Message = "Password changed successfully.";
            return View();
        }

  

        private static string HashPassword(string password)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        }
    }
}
