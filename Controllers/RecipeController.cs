using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using JamesThewProject.Data;
using JamesThewProject.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace JamesThewProject.Controllers
{
    public class RecipesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public RecipesController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Recipes/ManageRecipes
        public IActionResult ManageRecipes(string searchString, string sortOrder)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (role != "Admin" && role != "Manager")
            {
                return Forbid();
            }

            // Lấy danh sách công thức của admin dưới dạng queryable (bao gồm thông tin Creator)
            var recipes = _context.Recipes
                                  .Include(r => r.Creator)
                                  .AsQueryable();

            // Tìm kiếm theo tiêu đề hoặc mô tả (không phân biệt chữ hoa, chữ thường)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                recipes = recipes.Where(r =>
                    r.Title.ToLower().Contains(lowerSearch) ||
                    (r.Description != null && r.Description.ToLower().Contains(lowerSearch)));
            }

            // Sắp xếp theo lựa chọn của người dùng
            switch (sortOrder)
            {
                case "title_asc":
                    recipes = recipes.OrderBy(r => r.Title);
                    break;
                case "title_desc":
                    recipes = recipes.OrderByDescending(r => r.Title);
                    break;
                case "date_asc":
                    recipes = recipes.OrderBy(r => r.CreatedAt);
                    break;
                case "date_desc":
                    recipes = recipes.OrderByDescending(r => r.CreatedAt);
                    break;
                default:
                    // Mặc định sắp xếp theo ngày tạo giảm dần
                    recipes = recipes.OrderByDescending(r => r.CreatedAt);
                    break;
            }

            // Lưu lại giá trị tìm kiếm và sắp xếp vào ViewBag để hiển thị lại trong view
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            return View(recipes.ToList());
        }


        // GET: /Recipes/Create
        [HttpGet]
        public IActionResult Create()
        {
            // Chỉ cho phép Admin
            string role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (role != "Admin" && role != "Manager")
            {
                return Forbid();
            }

            var vm = new CreateOrEditRecipeViewModel
            {
                Servings = 1,
                IsPremium = false,
                AvailableIngredients = _context.Ingredients.OrderBy(i => i.Name).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult Create(CreateOrEditRecipeViewModel vm)
        {
            // Chỉ cho phép Admin
            string role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (role != "Admin" && role != "Manager")
            {
                return Forbid();
            }

            // Kiểm tra ModelState ban đầu
            if (!ModelState.IsValid)
            {
                // Nạp lại danh sách nguyên liệu để hiển thị trong form
                vm.AvailableIngredients = _context.Ingredients.OrderBy(i => i.Name).ToList();
                return View(vm);
            }

            // Load danh sách từ khóa nhạy cảm từ database (chuyển về chữ thường)
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
                // Nạp lại danh sách nguyên liệu để hiển thị trong form
                vm.AvailableIngredients = _context.Ingredients.OrderBy(i => i.Name).ToList();
                return View(vm);
            }

            var recipe = new Recipe
            {
                Title = vm.Title,
                Description = vm.Description,
                Instructions = vm.Instructions,
                Servings = vm.Servings,
                IsPremium = vm.IsPremium,
                IsHidden = vm.IsHidden,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                // Lấy CreatedBy từ Session; đảm bảo đã đăng nhập và role Admin
                CreatedBy = HttpContext.Session.GetInt32("UserId") ?? 0
            };

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(vm.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    vm.ImageFile.CopyTo(fileStream);
                }
                recipe.ImageUrl = "/uploads/" + uniqueFileName;
            }

            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            foreach (var si in vm.SelectedIngredients.Where(si => si.IngredientId > 0))
            {
                _context.RecipeIngredients.Add(new RecipeIngredient
                {
                    RecipeId = recipe.RecipeId,
                    IngredientId = si.IngredientId,
                    Quantity = si.Quantity
                });
            }
            _context.SaveChanges();

            return RedirectToAction("ManageRecipes");
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            // Lấy thông tin user hiện tại
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Tìm recipe
            var recipe = _context.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefault(r => r.RecipeId == id);

            if (recipe == null)
                return NotFound();

            // Chỉ cho phép chỉnh sửa nếu user hiện tại là người tạo công thức
            bool isOwner = (recipe.CreatedBy == currentUserId.Value);
            bool lockFields = !isOwner; // Nếu không phải chủ sở hữu thì khóa

            ViewBag.LockFields = lockFields;

            var vm = new CreateOrEditRecipeViewModel
            {
                RecipeId = recipe.RecipeId,
                Title = recipe.Title,
                Description = recipe.Description,
                Instructions = recipe.Instructions,
                Servings = recipe.Servings,
                IsPremium = recipe.IsPremium,
                IsHidden = recipe.IsHidden,
                ExistingImageUrl = recipe.ImageUrl,
                AvailableIngredients = _context.Ingredients.OrderBy(i => i.Name).ToList(),
                SelectedIngredients = recipe.RecipeIngredients
                    .Select(ri => new SelectedIngredientViewModel
                    {
                        RecipeIngredientId = ri.RecipeIngredientId,
                        IngredientId = ri.IngredientId,
                        Quantity = ri.Quantity
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult Edit(CreateOrEditRecipeViewModel vm)
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var recipe = _context.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefault(r => r.RecipeId == vm.RecipeId);

            if (recipe == null)
                return NotFound();

            bool isOwner = (recipe.CreatedBy == currentUserId.Value);
            bool lockFields = !isOwner; // Nếu không phải chủ sở hữu thì khóa

            // Nếu không phải chủ sở hữu, chỉ cập nhật các trường cho phép (ví dụ IsPremium, IsHidden)
            if (lockFields)
            {
                recipe.IsPremium = vm.IsPremium;
                recipe.IsHidden = vm.IsHidden;
            }
            else
            {
                // Cập nhật toàn bộ các trường nếu là chủ sở hữu
                recipe.Title = vm.Title;
                recipe.Description = vm.Description;
                recipe.Instructions = vm.Instructions;
                recipe.Servings = vm.Servings;
                recipe.IsPremium = vm.IsPremium;
                recipe.IsHidden = vm.IsHidden;

                // Upload ảnh nếu có
                if (vm.ImageFile != null && vm.ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(vm.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        vm.ImageFile.CopyTo(fileStream);
                    }
                    recipe.ImageUrl = "/uploads/" + uniqueFileName;
                }

                // Xóa các RecipeIngredients cũ và thêm mới
                var oldIngredients = recipe.RecipeIngredients.ToList();
                _context.RecipeIngredients.RemoveRange(oldIngredients);
                _context.SaveChanges();

                foreach (var si in vm.SelectedIngredients)
                {
                    if (si.IngredientId > 0)
                    {
                        var recipeIng = new RecipeIngredient
                        {
                            RecipeId = recipe.RecipeId,
                            IngredientId = si.IngredientId,
                            Quantity = si.Quantity
                        };
                        _context.RecipeIngredients.Add(recipeIng);
                    }
                }
            }

            _context.SaveChanges();
            return RedirectToAction("ManageRecipes");
        }
        // GET: /Recipes/FreeRecipes
        public IActionResult FreeRecipes(string searchString, string sortOrder)
        {
            // Lấy các công thức miễn phí (không phải Premium và không bị ẩn)
            var recipes = _context.Recipes
                 .Include(r => r.RecipeRatings)
                .Where(r => !r.IsPremium && !r.IsHidden);

            // Lọc theo từ khóa nếu có
            if (!string.IsNullOrEmpty(searchString))
            {
                recipes = recipes.Where(r =>
                    r.Title.Contains(searchString) ||
                    (r.Description != null && r.Description.Contains(searchString)));
            }

            // Sắp xếp dựa theo lựa chọn của người dùng
            switch (sortOrder)
            {
                case "title_asc":
                    recipes = recipes.OrderBy(r => r.Title);
                    break;
                case "title_desc":
                    recipes = recipes.OrderByDescending(r => r.Title);
                    break;
                case "date_asc":
                    recipes = recipes.OrderBy(r => r.CreatedAt);
                    break;
                case "date_desc":
                    recipes = recipes.OrderByDescending(r => r.CreatedAt);
                    break;
                default:
                    recipes = recipes.OrderByDescending(r => r.CreatedAt);
                    break;
            }

            return View(recipes.ToList());
        }
        // GET: /Recipes/PremiumRecipes
        public IActionResult PremiumRecipes(string searchString, string sortOrder)
        {
            // Retrieve user role from session (if needed for further logic)
            string userRole = HttpContext.Session.GetString("UserRole") ?? "User";
            // Optionally, you may want to check membership type if premium access should be restricted.
            // string membershipType = HttpContext.Session.GetString("MembershipType") ?? "Free";

            // Get all premium recipes that are not hidden
            var recipes = _context.Recipes
                 .Include(r => r.RecipeRatings)
                .Where(r => r.IsPremium && !r.IsHidden);

            // Apply search filter if a search string is provided
            if (!string.IsNullOrEmpty(searchString))
            {
                recipes = recipes.Where(r =>
                    r.Title.Contains(searchString) ||
                    (r.Description != null && r.Description.Contains(searchString)));
            }

            // Apply sorting based on the sortOrder parameter
            switch (sortOrder)
            {
                case "title_asc":
                    recipes = recipes.OrderBy(r => r.Title);
                    break;
                case "title_desc":
                    recipes = recipes.OrderByDescending(r => r.Title);
                    break;
                case "date_asc":
                    recipes = recipes.OrderBy(r => r.CreatedAt);
                    break;
                case "date_desc":
                    recipes = recipes.OrderByDescending(r => r.CreatedAt);
                    break;
                default:
                    recipes = recipes.OrderByDescending(r => r.CreatedAt);
                    break;
            }

            // Return the view "PremiumRecipes" (ensure the view is located at Views/Recipes/PremiumRecipes.cshtml)
            return View(recipes.ToList());
        }

        // GET: /Recipes/Details/5
        public IActionResult Details(int id)
        {
            // Lấy UserRole và các thông tin khác từ Session
            string userRole = HttpContext.Session.GetString("UserRole") ?? "User";
            string memberShipType = HttpContext.Session.GetString("MembershipType") ?? "Free";
            int? currentUserId = HttpContext.Session.GetInt32("UserId");

            // Truy vấn Recipe, bao gồm các mối quan hệ cần thiết
            var recipe = _context.Recipes
                .Include(r => r.RecipeRatings)
        .Include(r => r.RecipeIngredients)

            .ThenInclude(ri => ri.Ingredient)
        .Include(r => r.Creator)
        // Include luôn RecipeComments => ThenInclude User
        .Include(r => r.RecipeComments)
            .ThenInclude(rc => rc.User)
        .FirstOrDefault(r => r.RecipeId == id);

            if (recipe == null)
            {
                return NotFound();
            }

            // Xác định xem người dùng hiện tại có phải là chủ sở hữu của recipe không
            bool isOwner = currentUserId.HasValue && (recipe.CreatedBy == currentUserId.Value);

            // Nếu không phải Admin và không phải chủ sở hữu, kiểm tra thuộc tính IsHidden
            if (!isOwner && userRole != "Admin" && recipe.IsHidden)
            {
                return NotFound();
            }

            // Nếu recipe là cao cấp và người dùng không có quyền xem, chuyển sang view UpgradeAccount (hoặc thông báo phù hợp)
            if (!isOwner && recipe.IsPremium && memberShipType != "Premium" && userRole != "Admin")
            {
                return View("UpgradeAccount", recipe);
            }

            return View(recipe);
        }



        // Action cho công thức do user gửi
        public async Task<IActionResult> MySubmittedRecipes(string searchString, string sortOrder)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var recipesQuery = _context.Recipes.Include(r => r.RecipeRatings).Where(r => r.CreatedBy == userId);

            // Tìm kiếm (không phân biệt chữ hoa, chữ thường)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                recipesQuery = recipesQuery.Where(r =>
                    r.Title.ToLower().Contains(lowerSearch) ||
                    (r.Description != null && r.Description.ToLower().Contains(lowerSearch)));
            }

            // Sắp xếp theo lựa chọn của người dùng
            switch (sortOrder)
            {
                case "title_asc":
                    recipesQuery = recipesQuery.OrderBy(r => r.Title);
                    break;
                case "title_desc":
                    recipesQuery = recipesQuery.OrderByDescending(r => r.Title);
                    break;
                case "date_asc":
                    recipesQuery = recipesQuery.OrderBy(r => r.CreatedAt);
                    break;
                case "date_desc":
                    recipesQuery = recipesQuery.OrderByDescending(r => r.CreatedAt);
                    break;
                default:
                    recipesQuery = recipesQuery.OrderByDescending(r => r.CreatedAt);
                    break;
            }

            var recipes = await recipesQuery.ToListAsync();
            return View(recipes); // Sử dụng view "MySubmittedRecipes.cshtml"
        }
        [HttpPost]
        public IActionResult SubmitComment(int recipeId, string commentText)
        {
            // Kiểm tra xem user đã đăng nhập chưa
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Load danh sách từ khóa nhạy cảm từ database (ví dụ từ bảng BannedWords)
            var bannedWords = _context.BannedWords.Select(b => b.Word.ToLower()).ToList();
            var lowerComment = commentText.ToLower();
            var foundWords = bannedWords.Where(word => lowerComment.Contains(word)).ToList();

            if (foundWords.Any())
            {
                // Nếu có từ nhạy cảm, lưu thông báo lỗi vào TempData
                TempData["CommentError"] = "Your comment contains sensitive words: " + string.Join(", ", foundWords);
                // Redirect về trang Details, nơi bạn có thể hiển thị thông báo lỗi
                return RedirectToAction("Details", new { id = recipeId });
            }

            // Tìm recipe
            var recipe = _context.Recipes.FirstOrDefault(r => r.RecipeId == recipeId);
            if (recipe == null)
            {
                return NotFound("Recipe not found.");
            }

            // Tạo comment mới
            var newComment = new RecipeComment
            {
                RecipeId = recipeId,
                UserId = userId.Value,
                CommentText = commentText,
                CreatedAt = DateTime.Now,
                IsVisible = true // hoặc false nếu bạn muốn admin duyệt
            };

            _context.RecipeComments.Add(newComment);
            _context.SaveChanges();

            // Quay về trang Details
            return RedirectToAction("Details", new { id = recipeId });
        }


        // Action cho công thức yêu thích
        public async Task<IActionResult> MyFavoriteRecipes()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var favoriteRecipes = await _context.FavoriteRecipes
                .Include(fr => fr.Recipe)
                 .ThenInclude(r => r.RecipeRatings)
                .Where(fr => fr.UserId == userId)
                .Select(fr => fr.Recipe)
                .ToListAsync();

            return View(favoriteRecipes); // Sử dụng view "MyFavoriteRecipes.cshtml"
        }

        [HttpGet]
        public async Task<IActionResult> MyRecipes(string searchString, string sortOrder)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách công thức của user
            var recipesQuery = _context.Recipes.Include(r => r.RecipeRatings).Where(r => r.CreatedBy == userId);

            // Lọc theo từ khóa (không phân biệt chữ hoa, chữ thường)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                recipesQuery = recipesQuery.Where(r =>
                    r.Title.ToLower().Contains(lowerSearch) ||
                    (r.Description != null && r.Description.ToLower().Contains(lowerSearch)));
            }

            // Sắp xếp dựa theo lựa chọn của người dùng
            switch (sortOrder)
            {
                case "title_asc":
                    recipesQuery = recipesQuery.OrderBy(r => r.Title);
                    break;
                case "title_desc":
                    recipesQuery = recipesQuery.OrderByDescending(r => r.Title);
                    break;
                case "date_asc":
                    recipesQuery = recipesQuery.OrderBy(r => r.CreatedAt);
                    break;
                case "date_desc":
                    recipesQuery = recipesQuery.OrderByDescending(r => r.CreatedAt);
                    break;
                default:
                    recipesQuery = recipesQuery.OrderByDescending(r => r.CreatedAt);
                    break;
            }

            var userRecipes = await recipesQuery.ToListAsync();

            // Lấy danh sách công thức yêu thích của user
            var favoriteRecipes = await _context.FavoriteRecipes
                .Include(fr => fr.Recipe)
                .Where(fr => fr.UserId == userId)
                .Select(fr => fr.Recipe)
                .ToListAsync();

            // Lưu giá trị tìm kiếm và sắp xếp vào ViewBag để hiển thị lại trong view
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;
            ViewBag.FavoriteRecipes = favoriteRecipes;

            return View(userRecipes);
        }

        // Thêm công thức vào danh sách yêu thích của user
        [HttpPost]
        public IActionResult AddToFavorites(int recipeId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra nếu đã tồn tại thì không thêm nữa
            var existing = _context.FavoriteRecipes.FirstOrDefault(f => f.UserId == userId && f.RecipeId == recipeId);
            if (existing == null)
            {
                var fav = new FavoriteRecipe { UserId = userId.Value, RecipeId = recipeId };
                _context.FavoriteRecipes.Add(fav);
                _context.SaveChanges();
            }

            return RedirectToAction("Details", "Recipes", new { id = recipeId });
        }
        [HttpPost]
        public IActionResult SubmitRating(int recipeId, int rating)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (rating < 1 || rating > 5)
            {
                TempData["RatingError"] = "Rating must be between 1 and 5.";
                return RedirectToAction("Details", new { id = recipeId });
            }

            var existingRating = _context.RecipeRatings
                .FirstOrDefault(r => r.RecipeId == recipeId && r.UserId == userId.Value);

            if (existingRating != null)
            {
                existingRating.Rating = rating;
                existingRating.CreatedAt = DateTime.Now; // Cập nhật thời gian nếu cần
                TempData["RatingSuccess"] = "Your rating has been updated successfully.";
            }
            else
            {
                var newRating = new RecipeRating
                {
                    RecipeId = recipeId,
                    UserId = userId.Value,
                    Rating = rating,
                    CreatedAt = DateTime.Now
                };
                _context.RecipeRatings.Add(newRating);
                TempData["RatingSuccess"] = "Your rating has been submitted successfully.";
            }

            _context.SaveChanges();
            return RedirectToAction("Details", new { id = recipeId });
        }

        [HttpPost]
        public IActionResult RemoveFromFavorites(int recipeId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var fav = _context.FavoriteRecipes.FirstOrDefault(f => f.UserId == userId && f.RecipeId == recipeId);
            if (fav != null)
            {
                _context.FavoriteRecipes.Remove(fav);
                _context.SaveChanges();
            }

            // Chuyển hướng về trang MyFavoriteRecipes để hiển thị danh sách đã được cập nhật
            return RedirectToAction("MyFavoriteRecipes");
        }


    }
}
