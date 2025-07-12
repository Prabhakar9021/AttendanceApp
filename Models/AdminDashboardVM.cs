namespace AttendanceApp.Models
{
    
        public class AdminDashboardVM
        {
            public List<User> Employees { get; set; }
            public List<AttendanceRequest> PendingRequests { get; set; }
        }
    
}
