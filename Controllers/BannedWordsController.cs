using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JamesThewProject.Data;
using JamesThewProject.Models;
using System.Linq;
using System.Threading.Tasks;

namespace JamesThewProject.Controllers
{
    public class BannedWordsController : Controller
    {
        private readonly AppDbContext _context;

        public BannedWordsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /BannedWords/Index?searchString=...
        public async Task<IActionResult> Index(string searchString)
        {
            // Tạo truy vấn ban đầu
            var query = _context.BannedWords.AsQueryable();

            // Nếu có từ khóa tìm kiếm, lọc theo Word chứa searchString
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.Word.Contains(searchString));
            }

            // Sắp xếp theo chữ cái tăng dần
            var bannedWords = await query.OrderBy(b => b.Word).ToListAsync();

            // Lưu lại giá trị tìm kiếm để hiển thị trong view
            ViewBag.SearchString = searchString;

            return View(bannedWords);
        }

        // GET: /BannedWords/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /BannedWords/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BannedWord bannedWord)
        {
            if (ModelState.IsValid)
            {
                _context.BannedWords.Add(bannedWord);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(bannedWord);
        }

      

        // GET: /BannedWords/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var bannedWord = await _context.BannedWords.FindAsync(id);
            if (bannedWord == null)
                return NotFound();

            return View(bannedWord);
        }

        // POST: /BannedWords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bannedWord = await _context.BannedWords.FindAsync(id);
            if (bannedWord != null)
            {
                _context.BannedWords.Remove(bannedWord);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
