using Leave_system.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace Leave_system.Controllers
{
    public class LeaveController : Controller
    {
        private readonly IConfiguration _config;

        public LeaveController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost]
        public IActionResult ApplyLeave([FromForm] LeaveRequest leave)
        {
            if (leave == null)
            {
                return Json(new { success = false, message = "Model binding failed" });
            }

            string cs = _config.GetConnectionString("dbconn");

            using (SqlConnection con = new SqlConnection(cs))
            {
                string query = @"INSERT INTO dbo.LeaveRequests
        (FirstName, LastName, LeaveType, EmergencyContact, StartDate, EndDate, Reason, Status)
        VALUES
        (@FirstName, @LastName, @LeaveType, @EmergencyContact, @StartDate, @EndDate, @Reason, 'Pending')";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@FirstName", leave.FirstName);
                cmd.Parameters.AddWithValue("@LastName", leave.LastName);
                cmd.Parameters.AddWithValue("@LeaveType", leave.LeaveType);
                cmd.Parameters.AddWithValue("@EmergencyContact", leave.EmergencyContact);
                cmd.Parameters.AddWithValue("@StartDate", leave.StartDate);
                cmd.Parameters.AddWithValue("@EndDate", leave.EndDate);
                cmd.Parameters.AddWithValue("@Reason", leave.Reason);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return Json(new { success = true });
        }

    }
}
