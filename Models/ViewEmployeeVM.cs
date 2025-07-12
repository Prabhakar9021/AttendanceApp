using AttendanceApp.Shared.Enums;
using AttendanceApp.Models;
namespace AttendanceApp.Models
{
    public class ViewEmployeeVM
    {
        public User Employee { get; set; }
        public AttendanceRequest Request { get; set; }
        public bool AlreadyMarked { get; set; }
        public ShiftType CurrentShift { get; set; }
        public DateTime BaseDate { get; set; }
        public bool IsOvertimeEligible { get; set; }
        public List<ShiftType> MarkedShifts { get; set; }
        public bool SectionMismatch { get; set; }
        public AttendanceStatVM Stats { get; set; }  
       
    }
}  

