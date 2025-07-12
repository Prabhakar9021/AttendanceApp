using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace AttendanceApp.Models
{
    public class User : IdentityUser
    {
        public string EmployeeId {  get; set; }
        public string? FullName {  get; set; }
        public string? DeviceId {  get; set; }
        
    }
}
