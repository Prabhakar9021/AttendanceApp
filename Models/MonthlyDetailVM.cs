namespace AttendanceApp.Models
{
    public class MonthlyDetailVM
    {
        public User Employee { get; set; }
        public string Month { get; set; }
        public int Year { get; set; }
        public List<ShiftDayWiseVM> ShiftDays { get; set; }
    }
}
