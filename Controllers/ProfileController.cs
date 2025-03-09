using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using JamesThewProject.Data;
using JamesThewProject.Models;
using System;
using System.IO;
using System.Linq;

namespace JamesThewProject.Controllers
{
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Profile/EditProfile
        [HttpGet]
        public IActionResult EditProfile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.UserId == userId.Value);
            if (user == null)
                return RedirectToAction("Login", "Account");

            return View(user);
        }

        // POST: /Profile/EditProfile
        [HttpPost]
        public IActionResult EditProfile(User model, IFormFile? avatarFile)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.UserId == userId.Value);
            if (user == null)
                return NotFound();

            // Update personal details
            user.FullName = model.FullName;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;
            user.Address = model.Address;
            user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.Now;

            // Handle avatar file upload
            if (avatarFile != null && avatarFile.Length > 0)
            {
                // Create "uploads" folder under wwwroot if it doesn't exist
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique file name
                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(avatarFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    avatarFile.CopyTo(fileStream);
                }

                // Save the relative path to the database
                user.AvatarUrl = "/uploads/" + uniqueFileName;
            }

            _context.Update(user);
            _context.SaveChanges();

            return RedirectToAction("Profile", "Account");
        }

        // GET: /Profile/Index - Display user's profile
        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.FirstOrDefault(u => u.UserId == userId.Value);
            return View(user);
        }
    }
}
