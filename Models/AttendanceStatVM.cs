namespace AttendanceApp.Models
{
    public class AttendanceStatVM
    {
        public List<MonthStatVM> MonthlyStats { get; set; } = new();
        public Dictionary<string, List<ShiftDayWiseVM>> DayWiseShifts { get; set; } = new();
    }

    public class MonthStatVM
    {
        public string MonthYear { get; set; }
        public string Month => MonthYear.Split(' ')[0];
        public int Year => int.Parse(MonthYear.Split(' ')[1]);
        public int Present { get; set; }
        public int Overtime { get; set; }
        public int Absent { get; set; }
        public int TotalPresent => Present + Overtime;
    }

    public class ShiftDayWiseVM
    {
        public DateTime BaseDate { get; set; }
        public List<ShiftCellVM> Shifts { get; set; } = new();
        public bool IsAbsent { get; set; } // If no shift accepted
    }

    public class ShiftCellVM
    {
        public string ShiftName { get; set; }
        public string Status { get; set; } // "Present", "Overtime", "Empty"
        public string Section { get; set; }
    }
}
