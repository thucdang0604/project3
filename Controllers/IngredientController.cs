using Microsoft.AspNetCore.Mvc;
using JamesThewProject.Data;
using JamesThewProject.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace JamesThewProject.Controllers
{
    public class IngredientController : Controller
    {
        private readonly AppDbContext _context;

        public IngredientController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Manage(string searchString, string sortOrder)
        {
            string role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (role != "Admin" && role != "Manager")
            {
                return Forbid();
            }

            // Lấy danh sách nguyên liệu dưới dạng queryable
            var ingredients = _context.Ingredients.AsQueryable();

            // Lọc theo từ khóa nếu có (so sánh không phân biệt chữ hoa, chữ thường)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                ingredients = ingredients.Where(i => i.Name.ToLower().Contains(lowerSearch) ||
                                                       (i.DefaultUnit != null && i.DefaultUnit.ToLower().Contains(lowerSearch)));
            }

            // Sắp xếp theo lựa chọn của người dùng
            switch (sortOrder)
            {
                case "name_asc":
                    ingredients = ingredients.OrderBy(i => i.Name);
                    break;
                case "name_desc":
                    ingredients = ingredients.OrderByDescending(i => i.Name);
                    break;
                case "unit_asc":
                    ingredients = ingredients.OrderBy(i => i.DefaultUnit);
                    break;
                case "unit_desc":
                    ingredients = ingredients.OrderByDescending(i => i.DefaultUnit);
                    break;
                default:
                    // Sắp xếp mặc định theo tên tăng dần
                    ingredients = ingredients.OrderBy(i => i.Name);
                    break;
            }

            // Lưu lại giá trị tìm kiếm và sắp xếp vào ViewBag để hiển thị trong view (nếu cần)
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            return View(ingredients.ToList());
        }



        // GET: /Ingredient/Create
        [HttpGet]
        public IActionResult Create()
        {
            // Kiểm tra role
            string role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (role != "Admin" && role != "Manager")
            {
                return Forbid();
            }

            // Trả về view với 1 đối tượng Ingredient rỗng để tránh null
            return View(new Ingredient());
        }

        // POST: /Ingredient/Create
        [HttpPost]
        public IActionResult Create(Ingredient model)
        {
            // Kiểm tra role
            string role = HttpContext.Session.GetString("UserRole") ?? "User";
            if (role != "Admin" && role != "Manager")
            {
                return Forbid();
            }

            // Nếu dữ liệu không hợp lệ, trả về lại view kèm model để hiển thị lỗi
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Thêm vào DB
            _context.Ingredients.Add(model);
            _context.SaveChanges();

            // Chuyển hướng về trang Manage
            return RedirectToAction("Manage");
        }
    }
}
