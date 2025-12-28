using System.Collections.Generic;

namespace Leave_system.Models
{
    public class AdminViewModel
    {
        public List<User> Users { get; set; }
        public List<LeaveRequest> PendingLeaves { get; set; }
    }
}


