using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Models
{
    public class RegisterEmployeeViewModel
    {
        [Required]
        public string EmployeeId { get; set; } // 🔹 Custom ID entered by admin

        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, Phone]
        public string PhoneNumber { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
