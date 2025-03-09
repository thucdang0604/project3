using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using JamesThewProject.Data;
using JamesThewProject.Models;
using Microsoft.EntityFrameworkCore;
namespace YourProject.Controllers
{
    public class AnnouncementController : Controller
    {
        private readonly AppDbContext _context;

        public AnnouncementController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult ManageAnnouncements(string searchString, string sortOrder)
        {
            // Lấy danh sách các thông báo dưới dạng queryable
            var announcementsQuery = _context.Announcements.AsQueryable();

            // Lọc theo từ khóa tìm kiếm (theo Title hoặc Description)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                announcementsQuery = announcementsQuery.Where(a =>
                    a.Title.ToLower().Contains(lowerSearch) ||
                    (a.Description != null && a.Description.ToLower().Contains(lowerSearch)));
            }

            // Sắp xếp dựa theo lựa chọn của người dùng
            switch (sortOrder)
            {
                case "title_asc":
                    announcementsQuery = announcementsQuery.OrderBy(a => a.Title);
                    break;
                case "title_desc":
                    announcementsQuery = announcementsQuery.OrderByDescending(a => a.Title);
                    break;
                case "date_asc":
                    announcementsQuery = announcementsQuery.OrderBy(a => a.PostedDate);
                    break;
                case "date_desc":
                    announcementsQuery = announcementsQuery.OrderByDescending(a => a.PostedDate);
                    break;
                default:
                    announcementsQuery = announcementsQuery.OrderByDescending(a => a.PostedDate);
                    break;
            }

            // Tạo view model chứa danh sách thông báo
            var viewModel = new ManageContestsViewModel
            {
                Announcements = announcementsQuery.ToList()
            };

            // Lưu lại các giá trị tìm kiếm và sắp xếp (bạn cũng có thể lưu vào viewModel nếu có các thuộc tính tương ứng)
            ViewBag.SearchString = searchString;
            ViewBag.SortOrder = sortOrder;

            return View(viewModel);
        }

        // GET: /Announcement/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Announcement());
        }

        [HttpPost]
        public IActionResult Create(Announcement announcement)
        {
            if (ModelState.IsValid)
            {
                if (announcement.ImageFile != null && announcement.ImageFile.Length > 0)
                {
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(announcement.ImageFile.FileName);
                    string filePath = Path.Combine(folderPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        announcement.ImageFile.CopyTo(stream);
                    }
                    announcement.ImageUrl = "/images/" + fileName;
                }
                else
                {
                    announcement.ImageUrl = "/images/default.png";
                }

                announcement.PostedDate = DateTime.Now;
                _context.Announcements.Add(announcement);
                _context.SaveChanges();
                return RedirectToAction("ManageAnnouncements");
            }
            return View(announcement);
        }


        // GET: /Announcement/Index
        public IActionResult Index()
        {
            var announcements = _context.Announcements
                .OrderByDescending(a => a.PostedDate)
                .ToList();
            return View(announcements);
        }

        // GET: /Announcement/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var announcement = _context.Announcements.FirstOrDefault(a => a.Id == id);
            if (announcement == null)
            {
                return NotFound();
            }
            return View(announcement);
        }
        [HttpPost]
        public IActionResult Edit(Announcement announcement)
        {
            if (ModelState.IsValid)
            {
                var existing = _context.Announcements.FirstOrDefault(a => a.Id == announcement.Id);
                if (existing == null)
                {
                    return NotFound();
                }

                // Cập nhật các trường
                existing.Title = announcement.Title;
                existing.Description = announcement.Description;
                existing.PostedDate = announcement.PostedDate;
                existing.IsDisplayed = announcement.IsDisplayed;

                // Nếu có upload ảnh mới
                if (announcement.ImageFile != null && announcement.ImageFile.Length > 0)
                {
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(announcement.ImageFile.FileName);
                    string filePath = Path.Combine(folderPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        announcement.ImageFile.CopyTo(stream);
                    }
                    existing.ImageUrl = "/images/" + fileName;
                }

                _context.SaveChanges();
                return RedirectToAction("ManageAnnouncements");
            }
            else
            {
                // Khi ModelState không hợp lệ, lấy lại ImageUrl từ dữ liệu gốc
                var existing = _context.Announcements.AsNoTracking().FirstOrDefault(a => a.Id == announcement.Id);
                if (existing != null)
                {
                    announcement.ImageUrl = existing.ImageUrl;
                    ModelState.Remove("ImageUrl");
                }
                return View(announcement);
            }
        }

        // Public List: hiển thị danh sách các thông báo được cho phép (IsDisplayed = true)
        // GET: /Announcement/Index
        public IActionResult List(string searchString, string sortOrder)
        {
            // Lấy danh sách các thông báo được hiển thị
            var announcements = _context.Announcements.Where(a => a.IsDisplayed);

            // Tìm kiếm theo tiêu đề hoặc mô tả nếu có từ khóa
            if (!string.IsNullOrEmpty(searchString))
            {
                announcements = announcements.Where(a =>
                    a.Title.Contains(searchString) || a.Description.Contains(searchString));
            }

            // Sắp xếp dựa theo lựa chọn
            switch (sortOrder)
            {
                case "title_asc":
                    announcements = announcements.OrderBy(a => a.Title);
                    break;
                case "title_desc":
                    announcements = announcements.OrderByDescending(a => a.Title);
                    break;
                case "date_asc":
                    announcements = announcements.OrderBy(a => a.PostedDate);
                    break;
                case "date_desc":
                    announcements = announcements.OrderByDescending(a => a.PostedDate);
                    break;
                default:
                    announcements = announcements.OrderByDescending(a => a.PostedDate);
                    break;
            }

            return View(announcements.ToList());
        }


        // Public Details: hiển thị chi tiết 1 thông báo dựa theo id
        [HttpGet]
        public IActionResult Details(int id)
        {
            var announcement = _context.Announcements.FirstOrDefault(a => a.Id == id && a.IsDisplayed);
            if (announcement == null)
            {
                return NotFound();
            }
            return View(announcement);
        }
    }
}
