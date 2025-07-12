using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Models
{
    public class EditEmployeeViewModel
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string EmployeeId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, Phone]
        public string PhoneNumber { get; set; }
        public string? NewPassword {  get; set; }
    }
}
