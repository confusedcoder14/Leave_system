using Leave_system.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

public class AttendanceController : Controller
{
    private readonly AppDbContext _context;

    public AttendanceController(AppDbContext context)
    {
        _context = context;
    }

    // 🔹 STATUS
    [HttpGet]
    public IActionResult Status()
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        if (userId == 0) return Json(new { isPunchedIn = false });

        // Check if user has an active punch (no PunchOutTime yet)
        var att = _context.Attendance
            .FirstOrDefault(a => a.UserId == userId && a.PunchOutTime == null);

        if (att == null)
            return Json(new { isPunchedIn = false });

        return Json(new
        {
            isPunchedIn = true,
            punchInTime = att.PunchInDate + att.PunchInTime // combine for frontend
        });
    }


    // 🔹 PUNCH IN
    [HttpPost]
    public IActionResult PunchIn()
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        if (userId == 0) return Unauthorized();

        bool alreadyIn = _context.Attendance
            .Any(a => a.UserId == userId && a.PunchOutTime == null);

        if (alreadyIn) return BadRequest("Already punched in");

        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        var now = DateTime.Now;

        var att = new Attendance
        {
            UserId = userId,
            FullName = $"{user.FirstName} {user.LastName}",
            PunchInDate = now.Date,
            PunchInTime = now.TimeOfDay
        };

        _context.Attendance.Add(att);
        _context.SaveChanges();

        return Ok();
    }


    // 🔹 PUNCH OUT
    [HttpPost]
    public IActionResult PunchOut()
    {
        int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        if (userId == 0) return Unauthorized();

        var att = _context.Attendance
            .FirstOrDefault(a => a.UserId == userId && a.PunchOutTime == null);

        if (att == null) return BadRequest("Not punched in");

        var now = DateTime.Now;

        att.PunchOutDate = now.Date;
        att.PunchOutTime = now.TimeOfDay;

        // Calculate work hours
        var punchInDateTime = att.PunchInDate + att.PunchInTime;
        att.WorkHours = (decimal)(now - punchInDateTime).TotalHours;

        _context.SaveChanges();
        return Ok();
    }

    [HttpGet]
    public IActionResult GetAttendance()
    {
        var attendanceData = _context.Attendance
            .Select(a => new
            {
                a.AttendanceId,
                a.FullName,                
                PunchInDate = a.PunchInDate.ToString("yyyy-MM-dd"),
                PunchInTime = a.PunchInTime.ToString(@"hh\:mm"),
                PunchOutDate = a.PunchOutDate.HasValue ? a.PunchOutDate.Value.ToString("yyyy-MM-dd") : "",
                PunchOutTime = a.PunchOutTime.HasValue ? a.PunchOutTime.Value.ToString(@"hh\:mm") : "",
                WorkHours = a.WorkHours ?? 0
            }).ToList();

        return Json(attendanceData);
    }



}
