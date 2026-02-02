using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public class Attendance
    {
        public int AttendanceId { get; set; }
        public int UserId { get; set; }

       
        public string FullName { get; set; }

        
        public DateTime PunchInDate { get; set; }
        public TimeSpan PunchInTime { get; set; }

        public DateTime? PunchOutDate { get; set; }
        public TimeSpan? PunchOutTime { get; set; }

        public decimal? WorkHours { get; set; }

       
        [ForeignKey("UserId")]
        public User User { get; set; }
    }

}