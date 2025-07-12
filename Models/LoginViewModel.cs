using System.ComponentModel.DataAnnotations;

namespace AttendanceApp.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public bool RememberMe { get; set; }
    
        public double? Latitude { get; set; }
       
        public double? Longitude { get; set; }
    }
}
