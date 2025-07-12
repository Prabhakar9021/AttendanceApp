using AttendanceApp.Shared.Enums;
namespace AttendanceApp.Models
{
    public class AttendanceRequest
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public SectionType Section { get; set; }
        public ShiftType Shift { get; set; }

        public DateTime BaseDate { get; set; } // Always represents the shift start date

        public bool IsOvertime { get; set; } = false;

        public string Status { get; set; } // "Pending", "Accepted", "Rejected"
        public string? AdminRemark { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.Now;
    }
}
