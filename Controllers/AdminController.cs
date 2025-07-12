using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AttendanceApp.Data;
using AttendanceApp.Models;
using AttendanceApp.Models;
using AttendanceApp.Services;
using AttendanceApp.Shared.Enums;

namespace AttendanceApp.Controllers
{
    [Authorize(Roles="Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly AttendanceService _attendanceService;


        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager,AppDbContext context, AttendanceService attendanceService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _attendanceService = attendanceService;
        }

        public async Task<IActionResult> Index()
        {

            var allEmployees = await _context.Users.ToListAsync();
                
                

            var today = DateTime.Now.Date;
            var now = DateTime.Now.TimeOfDay;

            ShiftType currentShift;
            if (now >= TimeSpan.FromHours(8) && now < TimeSpan.FromHours(14))
                currentShift = ShiftType.Morning;
            else if (now >= TimeSpan.FromHours(14) && now < TimeSpan.FromHours(20))
                currentShift = ShiftType.Evening;
            else
                currentShift = ShiftType.Night;

            if (currentShift == ShiftType.Night && now < TimeSpan.FromHours(8))
                today = today.AddDays(-1);

            var pendingRequests = await _context.AttendanceRequests
                .Where(r => r.Status == "Pending" && r.BaseDate == today && r.Shift == currentShift)
                .ToListAsync();
            // Sort employees: those with requests come first
            var requestedEmployeeIds = pendingRequests.Select(r => r.UserId).ToHashSet();
            var sortedEmployees = allEmployees
                .OrderByDescending(e => requestedEmployeeIds.Contains(e.Id))
                .ToList();



            var model = new AdminDashboardVM
            {
                Employees = sortedEmployees,
                PendingRequests = pendingRequests
            };

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> ViewEmployee(string id)
        {
            var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (employee == null)
                return NotFound();

            var currentShift = GetCurrentShift(out DateTime baseDate);

            var request = await _context.AttendanceRequests
                .FirstOrDefaultAsync(r =>
                    r.UserId == id &&
                    r.BaseDate == baseDate &&
                    r.Shift == currentShift);

            var alreadyMarked = request != null && request.Status != "Pending";

            // Check how many shifts are accepted for base date
            var markedShifts = await _context.AttendanceRequests
                .Where(r => r.UserId == id && r.BaseDate == baseDate && r.Status == "Accepted")
                .Select(r => r.Shift)
                .ToListAsync();
            var stats = await _attendanceService.GetAttendanceStatsForUserAsync(id);
            var vm = new ViewEmployeeVM
            {
                Employee = employee,
                Request = request,
                AlreadyMarked = alreadyMarked,
                BaseDate = baseDate,
                CurrentShift = currentShift,
                MarkedShifts = markedShifts,
                Stats = stats
                
            };


            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            else
            {
                await _userManager.DeleteAsync(employee);
                TempData["Success"] = "Employee deleted successfully.";
                return RedirectToAction("Index");
            }   
           
            
            
        }
        [HttpGet]
        public async Task<IActionResult> EditEmployee(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new EditEmployeeViewModel
            {
                Id = user.Id,
                EmployeeId = user.EmployeeId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(EditEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            user.EmployeeId = model.EmployeeId;
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.UserName = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            // If admin entered a new password, reset it
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(model);
                }
            }

            TempData["Success"] = "✅ Employee updated successfully.";
            return RedirectToAction("ViewEmployee", new { id = model.Id });
            

        }
        [Authorize]
        public IActionResult DebugCheck()
        {
            return Content($"Authenticated: {User.Identity.IsAuthenticated}, Role(Admin): {User.IsInRole("Admin")}");
        }
        [HttpGet]
        public IActionResult RegisterEmployee()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterEmployee(RegisterEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check for duplicate EmployeeId
            var existingUserWithId = _userManager.Users.FirstOrDefault(u => u.EmployeeId == model.EmployeeId);
            if (existingUserWithId != null)
            {
                ModelState.AddModelError("EmployeeId", "Employee ID already exists.");
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmployeeId = model.EmployeeId,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                
                if (!await _roleManager.RoleExistsAsync("Employee"))
                    await _roleManager.CreateAsync(new IdentityRole("Employee"));

                // Assign role
                await _userManager.AddToRoleAsync(user, "Employee");

                TempData["Success"] = "✅ Employee registered successfully.";
                return RedirectToAction("RegisterEmployee","Admin");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetDevice(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            user.DeviceId = null; // reset device ID
            await _userManager.UpdateAsync(user);

            TempData["Message"] = "Device binding has been reset for the employee.";
            return RedirectToAction("ViewEmployee", new { id = id });
        }
        public async Task<IActionResult> AttendanceRequests()
        {
            var requests = await _context.AttendanceRequests
                .Include(a => a.User)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAttendanceStatus(int id, string actionType, string? remark)
        {
            var request = await _context.AttendanceRequests.FindAsync(id);

            if (request == null)
                return NotFound();

            if (request.Status != "Pending")
            {
                TempData["Message"] = "Request already processed.";
                return RedirectToAction("AttendanceRequests");
            }

            request.Status = actionType == "accept" ? "Accepted" : "Rejected";
            request.AdminRemark = remark ?? "";
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Request {request.Status} successfully.";
            return RedirectToAction("AttendanceRequests");
        }
        [HttpPost]
        public async Task<IActionResult> MarkAttendance(string UserId, ShiftType Shift, string Section, DateTime BaseDate, string Status)
        {
            var existing = await _context.AttendanceRequests
                .FirstOrDefaultAsync(x => x.UserId == UserId && x.Shift == Shift && x.BaseDate == BaseDate);

            if (existing != null && existing.Status != "Pending")
            {
                TempData["Error"] = "Attendance already marked.";
                return RedirectToAction("ViewEmployee", new { id = UserId });
            }

            if (existing != null)
            {
                existing.Status = Status;
                existing.AdminRemark = "Marked by admin";
            }
            else
            {
                var newRequest = new AttendanceRequest
                {
                    UserId = UserId,
                    Shift = Shift,
                    Section = Enum.Parse<SectionType>(Section),
                    BaseDate = BaseDate,
                    RequestedAt = DateTime.Now,
                    Status = Status,
                    AdminRemark = "Directly marked by admin"
                };
                _context.AttendanceRequests.Add(newRequest);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Attendance marked successfully.";
            return RedirectToAction("ViewEmployee", new { id = UserId });
        }
        private ShiftType GetCurrentShift(out DateTime baseDate)
        {
            var now = DateTime.Now;
            var time = now.TimeOfDay;

            ShiftType shift;
            if (time >= TimeSpan.FromHours(8) && time < TimeSpan.FromHours(14))
                shift = ShiftType.Morning;
            else if (time >= TimeSpan.FromHours(14) && time < TimeSpan.FromHours(20))
                shift = ShiftType.Evening;
            else
                shift = ShiftType.Night;

            // Handle night shift BaseDate adjustment
            if (shift == ShiftType.Night && time < TimeSpan.FromHours(8))
                baseDate = now.Date.AddDays(-1);
            else
                baseDate = now.Date;

            return shift;
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
