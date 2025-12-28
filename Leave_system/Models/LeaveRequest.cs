using System;

namespace Leave_system.Models
{
    public class LeaveRequest
    {
        public int? Id { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? LeaveType { get; set; }
        public string? EmergencyContact { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
    }

}
