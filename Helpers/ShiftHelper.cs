using System;
using AttendanceApp.Shared.Enums;

namespace AttendanceApp.Helpers
{
    public static class ShiftHelper
    {
        public static (DateTime Start, DateTime End) GetShiftTime(ShiftType shift, DateTime baseDate)
        {
            return shift switch
            {
                ShiftType.Morning => (baseDate.AddHours(8), baseDate.AddHours(14)),
                ShiftType.Evening => (baseDate.AddHours(14), baseDate.AddHours(20)),
                ShiftType.Night => (baseDate.AddHours(20), baseDate.AddDays(1).AddHours(8)),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}

