using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AttendanceApp.Data;
using AttendanceApp.Models;
using AttendanceApp.Shared.Enums;

namespace AttendanceApp.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public AttendanceController(AppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest(string section, ShiftType shift)
        {
            if (!Enum.TryParse<SectionType>(section, true, out var sectionType))
                return BadRequest("Invalid section.");

            var now = DateTime.Now.TimeOfDay;

            // Detect real-time shift
            ShiftType currentShift;
            if (now >= TimeSpan.FromHours(8) && now < TimeSpan.FromHours(14))
                currentShift = ShiftType.Morning;
            else if (now >= TimeSpan.FromHours(14) && now < TimeSpan.FromHours(20))
                currentShift = ShiftType.Evening;
            else
                currentShift = ShiftType.Night;

            if (shift != currentShift)
                return BadRequest("Only current shift is allowed now.");

            // Set base date
            var baseDate = DateTime.Now.Date;
            if (currentShift == ShiftType.Night && now < TimeSpan.FromHours(8))
                baseDate = baseDate.AddDays(-1);

            // Get current logged-in user
            var userId = _userManager.GetUserId(User); // Requires injecting UserManager<User>

            // Check if attendance already exists for this shift and base date
            var existing = await _context.AttendanceRequests
                .FirstOrDefaultAsync(a => a.UserId == userId &&
                                          a.BaseDate == baseDate &&
                                          a.Shift == currentShift);

            if (existing != null)
            {
                TempData["Message"] = "You have already submitted attendance for this shift.";
                return RedirectToAction("Dashboard", "Employee");
            }

            // Create new request
            var request = new AttendanceRequest
            {
                UserId = userId,
                Section = sectionType,
                Shift = shift,
                BaseDate = baseDate,
                Status = "Pending",
                RequestedAt = DateTime.Now
            };

            _context.AttendanceRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Attendance request submitted successfully!";
            return RedirectToAction("Dashboard", "Employee");
        }

    }
}
