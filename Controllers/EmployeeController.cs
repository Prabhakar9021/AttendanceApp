using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AttendanceApp.Data;
using AttendanceApp.Models;
using AttendanceApp.Services;
using AttendanceApp.Shared.Enums;

namespace AttendanceApp.Controllers
{
    [Authorize(Roles ="Employee")]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly AttendanceService _attendanceService;
        public EmployeeController(AppDbContext context, UserManager<User> userManager, AttendanceService attendanceService)
        {
            _context = context;
            _userManager = userManager;
            _attendanceService = attendanceService;
        }
        public async Task<IActionResult> Index()
        {
            
            var userId = _userManager.GetUserId(User);

            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            ShiftType currentShift;
            DateTime baseDate;

            if (now.TimeOfDay >= TimeSpan.FromHours(8) && now.TimeOfDay < TimeSpan.FromHours(14))
            {
                currentShift = ShiftType.Morning;
                baseDate = today;
            }
            else if (now.TimeOfDay >= TimeSpan.FromHours(14) && now.TimeOfDay < TimeSpan.FromHours(20))
            {
                currentShift = ShiftType.Evening;
                baseDate = today;
            }
            else
            {
                currentShift = ShiftType.Night;
                baseDate = (now.TimeOfDay >= TimeSpan.FromHours(20)) ? today : today.AddDays(-1);
            }

            var requests = await _context.AttendanceRequests
                .Where(r => r.UserId == userId && r.BaseDate == baseDate)
                .ToListAsync();

            var markedShifts = requests
                .Where(r => r.Status == "Accepted")
                .Select(r => r.Shift)
                .ToList();

            var existingRequest = requests
                .FirstOrDefault(r => r.Shift == currentShift);

            var pendingShifts = requests
                .Where(r => r.Status == "Pending")
                .Select(r => r.Shift)
                .ToList();

            var employee = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var stats = await _attendanceService.GetAttendanceStatsForUserAsync(employee.Id);
            var vm = new EmployeeDashboardVM
            {
                Employee = employee,
                BaseDate = baseDate,
                CurrentShift = currentShift,
                ExistingRequest = existingRequest,
                MarkedShifts = markedShifts,
                PendingRequests=pendingShifts,
                Stats = stats

            };

            return View(vm);
        }
        [HttpPost]
        public async Task<IActionResult> SendAttendanceRequest(ShiftType Shift, DateTime BaseDate, string Section)
        {
            var userId = _userManager.GetUserId(User);

            var existing = await _context.AttendanceRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Shift == Shift && r.BaseDate == BaseDate);

            if (existing != null)
            {
                TempData["Message"] = "You already sent a request or attendance is marked.";
                return RedirectToAction("Index");
            }

            var newRequest = new AttendanceRequest
            {
                UserId = userId,
                Shift = Shift,
                Section = Enum.Parse<SectionType>(Section),
                BaseDate = BaseDate,
                RequestedAt = DateTime.Now,
                Status = "Pending"
            };

            _context.AttendanceRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Request sent successfully.";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> MonthlyDetails(string userId, string month, int year)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            var stats = await _attendanceService.GetAttendanceStatsForUserAsync(userId);

            var key = $"{month} {year}";
            if (!stats.DayWiseShifts.ContainsKey(key))
                return View("NoData");

            var vm = new MonthlyDetailVM
            {
                Employee = user,
                Month = month,
                Year = year,
                ShiftDays = stats.DayWiseShifts[key]
            };

            return View(vm);
        }
        [HttpGet]
        public async Task<IActionResult> ExportMonthlyDetailsToExcel(string id, string month, int year)
        {
            var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (employee == null)
                return NotFound();

            var statVM = await _attendanceService.GetAttendanceStatsForUserAsync(id);
            var monthKey = $"{month} {year}";

            if (!statVM.DayWiseShifts.TryGetValue(monthKey, out var shiftDays))
                return NotFound("No data found for the selected month.");

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance Report");

            // Headers
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Morning (Section)";
            worksheet.Cell(1, 3).Value = "Evening (Section)";
            worksheet.Cell(1, 4).Value = "Night (Section)";
            worksheet.Cell(1, 5).Value = "Overall Status";

            int row = 2;

            foreach (var day in shiftDays.OrderBy(d => d.BaseDate))
            {
                worksheet.Cell(row, 1).Value = day.BaseDate.ToString("yyyy-MM-dd");

                if (day.IsAbsent)
                {
                    worksheet.Range(row, 2, row, 4).Merge().Value = "Absent";
                    worksheet.Cell(row, 5).Value = "Absent";
                }
                else
                {
                    string overall = "";

                    foreach (var shiftType in new[] { "Morning", "Evening", "Night" })
                    {
                        var shift = day.Shifts.FirstOrDefault(s => s.ShiftName == shiftType);
                        var col = shiftType == "Morning" ? 2 : shiftType == "Evening" ? 3 : 4;

                        if (shift != null && !string.IsNullOrEmpty(shift.Status))
                        {
                            var section = shift.Section ?? "-";
                            worksheet.Cell(row, col).Value = $"{shift.Status} ({section})";

                            overall += shift.Status == "Present" ? "P " : "O ";
                        }
                        else
                        {
                            worksheet.Cell(row, col).Value = "-";
                            overall += "- ";
                        }
                    }

                    worksheet.Cell(row, 5).Value = overall.Trim();
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            string fileName = $"Attendance_{employee.FullName}{month}{year}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

    }
}
