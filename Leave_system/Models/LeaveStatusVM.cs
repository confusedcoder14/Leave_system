namespace Leave_system.Models
{
    public class LeaveStatusVM
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string? reason { get; set; }
        public DateTime? endDate { get; set; }
    }
}

