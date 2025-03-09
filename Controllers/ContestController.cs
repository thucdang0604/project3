using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JamesThewProject.Data;
using JamesThewProject.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using JamesThewProject.Models.ViewModels;
using System.Net.Mail;
using System.Net;
using JamesThewProject.Services;

namespace JamesThewProject.Controllers
{
    public class ContestController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailSender _emailSender; // <-- thêm trường này
        public ContestController(AppDbContext context, IWebHostEnvironment environment, IEmailSender emailSender)
        {
            _context = context;
            _environment = environment;
            _emailSender = emailSender;  // gán từ constructor
        }

        #region Admin Actions

        // GET: /Contest/Create (Admin)
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin()) return Forbid();
            return View(new Contest());
        }

        [HttpPost]
        public async Task<IActionResult> Create(Contest contest)
        {
            if (!IsAdmin())
                return Forbid();

            var now = DateTime.Now;

            // Nếu người dùng không nhập giá trị (có thể là DateTime.MinValue), mới gán mặc định
            if (contest.StartDate == DateTime.MinValue)
            {
                contest.StartDate = now.AddHours(6); // mặc định là now + 6 giờ nếu không nhập
            }
            if (contest.EndDate == DateTime.MinValue)
            {
                contest.EndDate = now.AddDays(7); // mặc định là now + 7 ngày nếu không nhập
            }

            // Thực hiện kiểm tra tùy thuộc vào nghiệp vụ của bạn
            // Ví dụ, bạn có thể kiểm tra rằng EndDate phải sau StartDate:
            if (contest.EndDate <= contest.StartDate)
            {
                ModelState.AddModelError("EndDate", "End Date must be after Start Date.");
            }

            if (ModelState.IsValid)
            {
                contest.CreatedAt = now;
                contest.UpdatedAt = now;
                _context.Contests.Add(contest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageContests));
            }
            return View(contest);
        }


        [HttpGet]
        public async Task<IActionResult> ManageContests(string searchString, string sortOrder)
        {
            if (!IsAdmin())
                return Forbid();

            // Tạo truy vấn trên DbSet Contests
            var contestsQuery = _context.Contests.AsQueryable();

            // Lọc theo từ khóa tìm kiếm (theo tên cuộc thi hoặc mô tả)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                contestsQuery = contestsQuery.Where(c =>
                    c.ContestName.ToLower().Contains(lowerSearch) ||
                    (c.Description != null && c.Description.ToLower().Contains(lowerSearch)));
            }

            // Sắp xếp theo lựa chọn của người dùng
            switch (sortOrder)
            {
                case "name_asc":
                    contestsQuery = contestsQuery.OrderBy(c => c.ContestName);
                    break;
                case "name_desc":
                    contestsQuery = contestsQuery.OrderByDescending(c => c.ContestName);
                    break;
                case "start_asc":
                    contestsQuery = contestsQuery.OrderBy(c => c.StartDate);
                    break;
                case "start_desc":
                    contestsQuery = contestsQuery.OrderByDescending(c => c.StartDate);
                    break;
                default:
                    // Mặc định sắp xếp theo StartDate tăng dần
                    contestsQuery = contestsQuery.OrderBy(c => c.StartDate);
                    break;
            }

            // Lưu lại các giá trị tìm kiếm và sắp xếp để hiển thị lại trong view
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            var contests = await contestsQuery.ToListAsync();
            return View(contests);
        }


        // GET: /Contest/Edit/5 (Admin)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsAdmin()) return Forbid();

            var contest = await _context.Contests
                .Include(c => c.Submissions)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.ContestId == id);

            if (contest == null) return NotFound();

            return View(contest);
        }

        // POST: /Contest/Edit (Admin)
        [HttpPost]
        public async Task<IActionResult> Edit(Contest contest)
        {
            if (!IsAdmin()) return Forbid();

            if (ModelState.IsValid)
            {
                contest.UpdatedAt = DateTime.Now;
                _context.Contests.Update(contest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageContests));
            }
            return View(contest);
        }

        // GET: /Contest/DetailAdmin/{id} (Admin)
        [HttpGet]
        public async Task<IActionResult> DetailAdmin(int id)
        {
            if (!IsAdmin())
                return Forbid();

            // Load contest cùng với submissions và User
            var contest = await _context.Contests
                .Include(c => c.Submissions)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.ContestId == id);

            if (contest == null)
                return NotFound();

            // Tạo danh sách view model cho mỗi submission, bao gồm Recipe từ bảng Recipes
            var submissionRecipes = new List<SubmissionRecipeViewModel>();

            if (contest.Submissions != null)
            {
                foreach (var submission in contest.Submissions)
                {
                    var recipe = await _context.Recipes
                        .Include(r => r.RecipeIngredients)
                        .FirstOrDefaultAsync(r => r.RecipeId == submission.RecipeId);

                    if (recipe != null)
                    {
                        submissionRecipes.Add(new SubmissionRecipeViewModel
                        {
                            Submission = submission,
                            Recipe = recipe
                        });
                    }
                }
            }

            // Tạo composite view model
            var vm = new ContestDetailAdminViewModel
            {
                Contest = contest,
                SubmissionRecipes = submissionRecipes
            };

            return View(vm);
        }
        [HttpGet]
        public async Task<IActionResult> MarkWinner(int submissionId, int contestId)
        {
            if (!IsAdmin())
                return Forbid();

            var submission = await _context.ContestSubmissions
                .Include(s => s.User)  // Để lấy email của người gửi
                .Include(s => s.Contest)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId && s.ContestId == contestId);

            if (submission == null)
                return NotFound("Submission not found.");

            submission.IsWinner = true;
            _context.ContestSubmissions.Update(submission);
            await _context.SaveChangesAsync();

            // Gửi email chúc mừng
            if (!string.IsNullOrEmpty(submission.User?.Email))
            {
                string subject = $"Congratulations! You are the winner of {submission.Contest.ContestName}";
                string body = $"Dear {submission.User.Email},<br/><br/>" +
                              $"Congratulations! You have been selected as the winner of the contest \"{submission.Contest.ContestName}\".<br/>" +
                              "Thank you for your participation.<br/><br/>" +
                              "Best regards,<br/>JamesThewProject Team";
                try
                {
                    await _emailSender.SendEmailAsync(submission.User.Email, subject, body);
                }
                catch (Exception ex)
                {
                    // Log exception hoặc xử lý lỗi theo ý bạn
                    // Ví dụ: _logger.LogError(ex, "Error sending winner email");
                }
            }

            return RedirectToAction(nameof(DetailAdmin), new { id = contestId });
        }
        // GET: /Contest/ViewSubmissions/{id} (Admin)
        [HttpGet]
        public async Task<IActionResult> ViewSubmissions(int id)
        {
            if (!IsAdmin()) return Forbid();

            var submissions = await _context.ContestSubmissions
                .Where(s => s.ContestId == id)
                .Include(s => s.User)
                .Include(s => s.Contest)
                .ToListAsync();

            return View(submissions);
        }

        #endregion

        #region User Actions

        [HttpGet]
        public async Task<IActionResult> List(string searchString, string sortOrder)
        {
            // Lấy danh sách cuộc thi đang hoạt động (IsActive == true)
            var contestsQuery = _context.Contests
                .Include(c => c.Submissions)
                .Where(c => c.IsActive);

            // Lọc theo từ khóa tìm kiếm (theo tên hoặc mô tả)
            if (!string.IsNullOrEmpty(searchString))
            {
                contestsQuery = contestsQuery.Where(c =>
                    c.ContestName.Contains(searchString) ||
                    (c.Description != null && c.Description.Contains(searchString)));
            }

            // Sắp xếp dựa theo lựa chọn của người dùng
            switch (sortOrder)
            {
                case "name_asc":
                    contestsQuery = contestsQuery.OrderBy(c => c.ContestName);
                    break;
                case "name_desc":
                    contestsQuery = contestsQuery.OrderByDescending(c => c.ContestName);
                    break;
                case "start_asc":
                    contestsQuery = contestsQuery.OrderBy(c => c.StartDate);
                    break;
                case "start_desc":
                    contestsQuery = contestsQuery.OrderByDescending(c => c.StartDate);
                    break;
                default:
                    contestsQuery = contestsQuery.OrderBy(c => c.StartDate);
                    break;
            }

            var contests = await contestsQuery.ToListAsync();
            return View(contests);
        }



        public async Task<IActionResult> DetailUser(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var contest = await _context.Contests
                .Include(c => c.Submissions)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.ContestId == id);
            if (contest == null) return NotFound();

            bool hasSubmitted = contest.Submissions.Any(s => s.UserId == userId);
            ViewBag.HasSubmitted = hasSubmitted;

            var vm = new ContestSubmissionRecipeViewModel
            {
                Contest = contest,
                RecipeViewModel = new CreateOrEditRecipeViewModel
                {
                    AvailableIngredients = _context.Ingredients.OrderBy(i => i.Name).ToList()
                }
            };

            return View(vm);
        }


        // GET: /Contest/SubmitEntry/{id}
        [HttpGet("Contest/SubmitEntry/{id}")]
        public async Task<IActionResult> SubmitEntryGet(int id)
        {
            // Tìm contest
            var contest = await _context.Contests.FindAsync(id);
            if (contest == null)
                return NotFound("Contest not found.");

            // Đưa info contest vào ViewBag (nếu cần hiển thị trong View)
            ViewBag.Contest = contest;

            // Tạo model "CreateOrEditRecipeViewModel"
            var vm = new CreateOrEditRecipeViewModel
            {
                AvailableIngredients = await _context.Ingredients
                    .OrderBy(i => i.Name).ToListAsync()
            };

            // Trả về view "SubmitEntry.cshtml" (khai báo @model CreateOrEditRecipeViewModel)
            return View("SubmitEntry", vm);
        }


        // POST: /Contest/SubmitEntry/{id}
        [HttpPost("Contest/SubmitEntry/{id}")]
        public async Task<IActionResult> SubmitEntryPost(int id, CreateOrEditRecipeViewModel vm)
        {
            // Kiểm tra contest
            var contest = await _context.Contests.FindAsync(id);
            if (contest == null)
                return NotFound("Contest not found.");

            // Nếu form chưa hợp lệ
            if (!ModelState.IsValid)
            {
                // Nạp lại danh sách ingredient
                vm.AvailableIngredients = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync();
                // Giữ contest info để hiển thị
                ViewBag.Contest = contest;
                return View("SubmitEntry", vm);
            }

            // Load danh sách từ khóa nhạy cảm từ database
            var bannedWords = _context.BannedWords.Select(b => b.Word.ToLower()).ToList();
            var errors = new System.Collections.Generic.List<string>();

            // Kiểm tra Title
            if (!string.IsNullOrEmpty(vm.Title) && bannedWords.Any(word => vm.Title.ToLower().Contains(word)))
            {
                errors.Add("Title contains banned words.");
            }
            // Kiểm tra Description
            if (!string.IsNullOrEmpty(vm.Description) && bannedWords.Any(word => vm.Description.ToLower().Contains(word)))
            {
                errors.Add("Description contains banned words.");
            }
            // Kiểm tra Instructions
            if (!string.IsNullOrEmpty(vm.Instructions) && bannedWords.Any(word => vm.Instructions.ToLower().Contains(word)))
            {
                errors.Add("Instructions contains banned words.");
            }

            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
                vm.AvailableIngredients = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync();
                // Giữ contest info để hiển thị
                ViewBag.Contest = contest;
                return View("SubmitEntry", vm);
            }

            // Lấy userId từ Session (nếu null => fix cứng = 1)
            int userId = HttpContext.Session.GetInt32("UserId") ?? 1;

            // Tạo Recipe mới
            var newRecipe = new Recipe
            {
                Title = vm.Title,
                Description = vm.Description,
                Instructions = vm.Instructions,
                Servings = vm.Servings,
                IsPremium = true,
                IsHidden = true,
                CreatedBy = userId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Nếu có upload file ảnh
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder); // Tạo thư mục nếu chưa có
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(vm.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.ImageFile.CopyToAsync(stream);
                }
                newRecipe.ImageUrl = "/uploads/" + uniqueFileName;
            }

            // Lưu Recipe
            _context.Recipes.Add(newRecipe);
            await _context.SaveChangesAsync();

            // Lưu RecipeIngredients
            if (vm.SelectedIngredients != null && vm.SelectedIngredients.Any())
            {
                foreach (var sel in vm.SelectedIngredients)
                {
                    if (sel.IngredientId > 0)
                    {
                        var recipeIng = new RecipeIngredient
                        {
                            RecipeId = newRecipe.RecipeId,
                            IngredientId = sel.IngredientId,
                            Quantity = sel.Quantity
                        };
                        _context.RecipeIngredients.Add(recipeIng);
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Tạo ContestSubmission (nếu muốn gắn công thức vào contest)
            var submission = new ContestSubmission
            {
                ContestId = id,
                UserId = userId,
                SubmissionContent = $"Recipe: {newRecipe.Title}",
                RecipeId = newRecipe.RecipeId, // Gán RecipeId
                SubmittedAt = DateTime.Now,
                IsWinner = false
            };
            _context.ContestSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Your recipe submission has been saved!";
            return RedirectToAction("List");
        }

        // ===========================
        // PHẦN TEST: Gửi công thức (Recipe) RIÊNG – chưa gán vào bài dự thi nào
        // GET: /Contest/SubmitRecipe (User)
        [HttpGet("Contest/SubmitRecipe")]
        public IActionResult SubmitRecipeTest()
        {
            var vm = new CreateOrEditRecipeViewModel();
            vm.AvailableIngredients = _context.Ingredients.OrderBy(i => i.Name).ToList();
            return View("SubmitRecipe", vm);
        }

        // POST: /Contest/SubmitRecipe (User)
        [HttpPost("Contest/SubmitRecipe")]
        public async Task<IActionResult> SubmitRecipeTest(CreateOrEditRecipeViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AvailableIngredients = _context.Ingredients.OrderBy(i => i.Name).ToList();
                return View("SubmitRecipe", vm);
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 1;

            Recipe newRecipe = new Recipe
            {
                Title = vm.Title,
                Instructions = vm.Instructions,
                Servings = vm.Servings,
                IsPremium = vm.IsPremium,
                IsHidden = vm.IsHidden,
                CreatedBy = userId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Xử lý upload ảnh nếu có
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                string uploadsFolder = System.IO.Path.Combine(_environment.WebRootPath, "uploads");
                if (!System.IO.Directory.Exists(uploadsFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadsFolder);
                }
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + System.IO.Path.GetFileName(vm.ImageFile.FileName);
                string filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await vm.ImageFile.CopyToAsync(stream);
                }
                newRecipe.ImageUrl = "/uploads/" + uniqueFileName;
            }

            _context.Recipes.Add(newRecipe);
            await _context.SaveChangesAsync();

            if (vm.SelectedIngredients != null && vm.SelectedIngredients.Any())
            {
                foreach (var sel in vm.SelectedIngredients)
                {
                    if (sel.IngredientId > 0)
                    {
                        var recipeIngredient = new RecipeIngredient
                        {
                            RecipeId = newRecipe.RecipeId,
                            IngredientId = sel.IngredientId,
                            Quantity = sel.Quantity
                        };
                        _context.RecipeIngredients.Add(recipeIngredient);
                    }
                }
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Your recipe has been submitted successfully!";
            return RedirectToAction("List");
        }
        // ===========================
        [HttpGet("Contest/EditSubmission/{submissionId}")]
        public async Task<IActionResult> EditSubmission(int submissionId)
        {
            // Lấy bài dự thi của user, bao gồm thông tin của Contest (không Include Recipe vì không có navigation property)
            var submission = await _context.ContestSubmissions
                .Include(s => s.Contest)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null)
                return NotFound("Submission not found.");

            // Kiểm tra thời gian contest: nếu contest đã kết thúc, không cho chỉnh sửa
            if (DateTime.UtcNow > submission.Contest.EndDate)
            {
                TempData["Message"] = "Contest has ended. Editing is not allowed.";
                return RedirectToAction("DetailUser", new { id = submission.Contest.ContestId });
            }

            // Kiểm tra userId từ session và đảm bảo chỉ chủ sở hữu mới có thể sửa
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || submission.UserId != userId)
            {
                return Forbid();
            }

            // Lấy thông tin Recipe từ bảng Recipes theo RecipeId
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.RecipeId == submission.RecipeId);

            if (recipe == null)
                return NotFound("Recipe not found.");

            // Tạo view model dựa trên Recipe
            var vm = new CreateOrEditRecipeViewModel
            {
                RecipeId = recipe.RecipeId,  // Nếu view model có thuộc tính RecipeId
                Title = recipe.Title,
                Description = recipe.Description,
                Instructions = recipe.Instructions,
                Servings = recipe.Servings,
                IsPremium = recipe.IsPremium,
                IsHidden = recipe.IsHidden,
                AvailableIngredients = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync(),
                SelectedIngredients = recipe.RecipeIngredients?
                    .Select(ri => new SelectedIngredientViewModel
                    {
                        IngredientId = ri.IngredientId,
                        Quantity = ri.Quantity
                    })
                    .ToList() ?? new List<SelectedIngredientViewModel>()
            };

            return View("EditSubmission", vm);
        }

        [HttpPost("Contest/EditSubmission/{submissionId}")]
        public async Task<IActionResult> EditSubmission(int submissionId, CreateOrEditRecipeViewModel vm)
        {
            // Lấy bài dự thi, bao gồm thông tin của Contest (không include Recipe vì không có navigation property)
            var submission = await _context.ContestSubmissions
                .Include(s => s.Contest)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null)
                return NotFound("Submission not found.");

            // Kiểm tra contest: nếu contest đã kết thúc, không cho chỉnh sửa
            if (DateTime.UtcNow > submission.Contest.EndDate)
            {
                TempData["Message"] = "Contest has ended. Editing is not allowed.";
                return RedirectToAction("DetailUser", new { id = submission.Contest.ContestId });
            }

            // Kiểm tra userId: chỉ chủ sở hữu mới được sửa
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || submission.UserId != userId)
            {
                return Forbid();
            }

            // Kiểm tra ModelState
            if (!ModelState.IsValid)
            {
                vm.AvailableIngredients = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync();
                return View("EditSubmission", vm);
            }

            // --- BẮT ĐẦU PHẦN KIỂM TRA TỪ KHÓA NHẠY CẢM ---
            var bannedWords = _context.BannedWords.Select(b => b.Word.ToLower()).ToList();
            var errors = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(vm.Title) && bannedWords.Any(word => vm.Title.ToLower().Contains(word)))
            {
                errors.Add("Title contains banned words.");
            }
            if (!string.IsNullOrEmpty(vm.Description) && bannedWords.Any(word => vm.Description.ToLower().Contains(word)))
            {
                errors.Add("Description contains banned words.");
            }
            if (!string.IsNullOrEmpty(vm.Instructions) && bannedWords.Any(word => vm.Instructions.ToLower().Contains(word)))
            {
                errors.Add("Instructions contains banned words.");
            }

            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
                vm.AvailableIngredients = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync();
                return View("EditSubmission", vm);
            }
            // --- KẾT THÚC PHẦN KIỂM TRA ---

            // Lấy thông tin Recipe từ bảng Recipes theo RecipeId của submission
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.RecipeId == submission.RecipeId);

            if (recipe == null)
            {
                return NotFound("Recipe not found.");
            }

            // Cập nhật thông tin của Recipe
            recipe.Title = vm.Title;
            recipe.Description = vm.Description;
            recipe.Instructions = vm.Instructions;
            recipe.Servings = vm.Servings;
            recipe.IsPremium = vm.IsPremium;
            recipe.IsHidden = vm.IsHidden;
            recipe.UpdatedAt = DateTime.UtcNow;

            // Xử lý upload ảnh nếu có
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(vm.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await vm.ImageFile.CopyToAsync(stream);
                }
                recipe.ImageUrl = "/uploads/" + uniqueFileName;
            }

            // Xóa RecipeIngredients cũ
            var oldIngredients = recipe.RecipeIngredients.ToList();
            _context.RecipeIngredients.RemoveRange(oldIngredients);
            await _context.SaveChangesAsync();

            // Thêm lại RecipeIngredients từ view model
            if (vm.SelectedIngredients != null && vm.SelectedIngredients.Any())
            {
                foreach (var sel in vm.SelectedIngredients)
                {
                    if (sel.IngredientId > 0)
                    {
                        var newRecipeIng = new RecipeIngredient
                        {
                            RecipeId = recipe.RecipeId,
                            IngredientId = sel.IngredientId,
                            Quantity = sel.Quantity
                        };
                        _context.RecipeIngredients.Add(newRecipeIng);
                    }
                }
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Your submission has been updated!";
            return RedirectToAction("DetailUser", new { id = submission.Contest.ContestId });
        }



        #endregion

        // Hàm tiện ích kiểm tra Admin
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole") ?? "User";
            return role == "Admin" || role == "Manager";
        }
    }
}
