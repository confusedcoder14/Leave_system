using System.ComponentModel.DataAnnotations;

namespace Leave_system.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public DateTime JoinedOn { get; set; } = DateTime.Now;

        public string Location { get; set; } = "Gurugram, Haryana";

    }
}