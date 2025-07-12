using AttendanceApp.Shared.Enums;

namespace AttendanceApp.Models
{
    public class EmployeeDashboardVM
    {
        public User Employee { get; set; }
        public ShiftType? CurrentShift { get; set; }
        public DateTime BaseDate { get; set; }
        public AttendanceRequest ExistingRequest { get; set; }
        public List<ShiftType> MarkedShifts { get; set; } = new();
        public List<ShiftType> PendingRequests { get; set; } = new();
        public AttendanceStatVM Stats { get; set; }

    }
}
