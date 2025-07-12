using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AttendanceApp.Data;
using AttendanceApp.Models;
using AttendanceApp.Shared.Enums;

namespace AttendanceApp.Services
{
    public class AttendanceService
    {
        private readonly AppDbContext _context;

        public AttendanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AttendanceStatVM> GetAttendanceStatsForUserAsync(string userId)
        {
            var allRequests = await _context.AttendanceRequests
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var baseDates = allRequests.Select(r => r.BaseDate).Distinct().ToList();
            if (!baseDates.Any())
                baseDates.Add(DateTime.Today);

            var minDate = baseDates.Min();
            var maxDate = baseDates.Max();

            var statVM = new AttendanceStatVM();

            for (var date = minDate; date <= maxDate; date = date.AddDays(1))
            {
                if (date == DateTime.Today) continue;
                var requestsOnDate = allRequests.Where(r => r.BaseDate == date && r.Status == "Accepted").ToList();
                var shiftCount = requestsOnDate.Count;
                string monthYear = date.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

                var monthStat = statVM.MonthlyStats.FirstOrDefault(x => x.MonthYear == monthYear);
                if (monthStat == null)
                {
                    monthStat = new MonthStatVM
                    {
                        MonthYear = monthYear,
                        Present = 0,
                        Overtime = 0,
                        Absent = 0
                    };
                    statVM.MonthlyStats.Add(monthStat);
                }

                if (shiftCount == 0)
                {
                    monthStat.Absent += 1;
                }
                else
                {
                    monthStat.Present += 1;
                    if (shiftCount > 1)
                        monthStat.Overtime += (shiftCount - 1);
                }

                // Build day-wise shift status
                var shiftCells = new List<ShiftCellVM>();
                foreach (ShiftType shift in Enum.GetValues(typeof(ShiftType)))
                {
                    var shiftRequest = requestsOnDate.FirstOrDefault(r => r.Shift == shift);
                    if (shiftRequest != null)
                    {
                        var index = requestsOnDate.IndexOf(shiftRequest);
                        shiftCells.Add(new ShiftCellVM
                        {
                            ShiftName = shift.ToString(),
                            Status = index == 0 ? "Present" : "Overtime",
                            Section = shiftRequest.Section.ToString()
                        });
                    }
                    else
                    {
                        shiftCells.Add(new ShiftCellVM
                        {
                            ShiftName = shift.ToString(),
                            Status = "",
                            Section = ""
                        });
                    }
                }

                if (!statVM.DayWiseShifts.ContainsKey(monthYear))
                    statVM.DayWiseShifts[monthYear] = new List<ShiftDayWiseVM>();

                statVM.DayWiseShifts[monthYear].Add(new ShiftDayWiseVM
                {
                    BaseDate = date,
                    IsAbsent = shiftCount == 0,
                    Shifts = shiftCells
                });
            }

            return statVM;
        }
    }
}
    

