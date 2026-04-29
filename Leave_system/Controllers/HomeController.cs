using Leave_system.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Leave_system.Hubs;

namespace Leave_system.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context, IHubContext<LeaveHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignUp(User user, string confirmPassword)
        {
            if (!ModelState.IsValid)
                return View(user);
            if (user.Password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match";
                return View(user);
            }
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ViewBag.Error = "Email already exists";
                return View(user);
            }
            string sql = "EXEC sp_InsertUser @FirstName, @LastName, @Phone, @Email, @Password, @Location, @JoinedOn";
            _context.Database.ExecuteSqlRaw(sql,
                new SqlParameter("@FirstName", user.FirstName),
                new SqlParameter("@LastName", user.LastName),
                new SqlParameter("@Phone", user.Phone),
                new SqlParameter("@Email", user.Email),
                new SqlParameter("@Password", user.Password),
                new SqlParameter("@Location", "Gurugram, Haryana"),
                new SqlParameter("@JoinedOn", DateTime.Now)
            );
            ViewBag.Success = true;
            return View();
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user != null && user.Password == password)
            {
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetInt32("UserId", user.Id);

                return RedirectToAction("User");
            }

            ViewBag.UserError = "Invalid email or password";   // ✅ change
            ViewBag.ActiveTab = "user";                        // ✅ add
            return View("Login");                              // ✅ important
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }



        public IActionResult User()
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }


        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> ApplyLeave([FromForm] LeaveRequest leave)
        {
            if (leave == null)
                return Json(new { success = false });

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false });

            leave.UserId = userId.Value;
            leave.Status = "Pending";
            leave.CreatedAt = DateTime.Now;

            _context.LeaveRequests.Add(leave);
            _context.SaveChanges();

            // 🔥 SIGNALR BROADCAST
            await _hub.Clients.All.SendAsync("NewLeave", new
            {
                name = leave.FirstName + " " + leave.LastName,
                leaveType = leave.LeaveType,
                startDate = leave.StartDate,
                endDate = leave.EndDate,
                reason = leave.Reason
            });

            return Json(new { success = true });
        }



        [HttpGet]
        public IActionResult GetLeaveHistory()
        {
            var email = HttpContext.Session.GetString("UserEmail");

            if (email == null)
                return Json(new { success = false, message = "User not logged in." });

            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
                return Json(new { success = false, message = "User not found." });

            // Get leave requests for this user only
            var leaveHistory = _context.LeaveRequests
                .Where(l => l.Id == user.Id)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.StartDate,
                    l.EndDate,
                    l.Reason,
                    l.Status,
                    l.CreatedAt
                }).ToList();

            return Json(new { success = true, data = leaveHistory });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminLogin(string admin, string password)
        {
            if (admin == "admin" && password == "password")
            {
                HttpContext.Session.SetString("Admin", "true");
                return RedirectToAction("Admin");
            }

            ViewBag.AdminError = "Invalid admin credentials"; // ✅
            ViewBag.ActiveTab = "admin";                      // ✅
            return View("Login");                             // ✅
        }

        public IActionResult Admin()
        {
            var isAdmin = HttpContext.Session.GetString("Admin");

            if (isAdmin != "true")
            {
                return RedirectToAction("Login");
            }

            var model = new AdminViewModel
            {
                Users = _context.Users.ToList(),
                PendingLeaves = _context.LeaveRequests
                    .Where(l => l.Status == "Pending")
                    .ToList()
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult SaveUser(User user)
        {
            if (user.Id == 0)
            {
                user.JoinedOn = DateTime.Now;
                _context.Users.Add(user);
                TempData["SuccessMessage"] = "User created successfully!";
            }
            else
            {
                var dbUser = _context.Users.FirstOrDefault(x => x.Id == user.Id);
                if (dbUser == null) return NotFound();

                dbUser.FirstName = user.FirstName;
                dbUser.LastName = user.LastName;
                dbUser.Email = user.Email;
                dbUser.Phone = user.Phone;
                dbUser.Password = user.Password;

                TempData["SuccessMessage"] = "User updated successfully!";
            }

            _context.SaveChanges();
            return RedirectToAction("Admin");
        }


        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> SaveLeave(LeaveRequest model)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == model.UserId);

            var leave = new LeaveRequest
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                LeaveType = model.LeaveType,
                EmergencyContact = model.EmergencyContact,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.LeaveRequests.Add(leave);
            _context.SaveChanges();

            // 🔥 SIGNALR HERE
            await _hub.Clients.All.SendAsync("NewLeave", new
            {
                name = leave.FirstName + " " + leave.LastName,
                leaveType = leave.LeaveType,
                startDate = leave.StartDate,
                endDate = leave.EndDate,
                reason = leave.Reason
            });

            return RedirectToAction("Admin");
        }




        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> UpdateLeaveStatus([FromBody] LeaveStatusVM model)
        {
            var leave = _context.LeaveRequests.FirstOrDefault(l => l.Id == model.Id);
            if (leave == null)
                return Json(new { success = false });

            leave.Status = model.Status;
            _context.SaveChanges();

            // 🔥 SIGNALR TO USER
            await _hub.Clients.All.SendAsync("LeaveStatusUpdate", new
            {
                name = leave.FirstName + " " + leave.LastName,
                status = leave.Status,
                leaveType = leave.LeaveType,
                startDate = leave.StartDate
            });

            return Json(new { success = true });
        }


        [HttpGet]
        public IActionResult GetReports()
        {
            var data = _context.LeaveRequests
                .Where(l => l.Status == "Approved" || l.Status == "Rejected")
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    firstName = l.FirstName ?? "",
                    lastName = l.LastName ?? "",
                    leaveType = l.LeaveType ?? "",
                    emergencyContact = l.EmergencyContact ?? "",
                    startDate = l.StartDate,
                    endDate = l.EndDate,
                    reason = l.Reason ?? "",
                    status = l.Status ?? "",
                    createdAt = l.CreatedAt
                })

                .ToList();

            return Json(data);
        }

        [HttpPost]
        public IActionResult UpdateProfile([FromBody] User model)
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (email == null)
                return Json(new { success = false });

            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return Json(new { success = false });

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Phone = model.Phone;
            user.Email = model.Email;
            user.Location = model.Location;

            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetMyReports()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Unauthorized();

            var data = _context.LeaveRequests
    .Where(l => l.UserId == userId)  // Keep all statuses
    .OrderByDescending(l => l.CreatedAt)
    .Select(l => new
    {
        leaveType = l.LeaveType ?? "",
        startDate = l.StartDate,
        endDate = l.EndDate,
        reason = l.Reason ?? "",
        status = l.Status ?? "",
        createdAt = l.CreatedAt
    })
    .ToList();


            return Json(data);
        }


        [HttpGet]
        public IActionResult GetLeaveNotifications()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            // Get the last few leaves that are Approved/Rejected but not yet notified
            var notifications = _context.LeaveRequests
                .Where(l => l.UserId == userId && (l.Status == "Approved" || l.Status == "Rejected"))
                .OrderByDescending(l => l.CreatedAt)
                .Take(5) // last 5 notifications
                .Select(l => new
                {
                    leaveDate = l.StartDate,
                    leaveType = l.LeaveType,
                    status = l.Status
                })
                .ToList();

            return Json(notifications);
        }

        [HttpGet]
        public IActionResult GetDashboardCounts()
        {
            var pendingLeaves = _context.LeaveRequests.Count(l => l.Status == "Pending");
            var teamMembers = _context.Users.Count();
            var today = DateTime.Today;
            var todayAttendance = _context.Attendance.Count(a => a.PunchInDate.Date == today);

            return Json(new { pendingLeaves, teamMembers, todayAttendance });
        }


        public IActionResult Dashboard()
        {
            var today = DateTime.Today;

            // DB se counts
            int pendingCount = _context.LeaveRequests.Count(l => l.Status == "Pending");
            int userCount = _context.Users.Count();
            int todayAttendanceCount = _context.Attendance.Count(a => a.PunchInDate.Date == today);

            // Send to view via ViewBag
            ViewBag.PendingCount = pendingCount;
            ViewBag.UserCount = userCount;
            ViewBag.TodayAttendance = todayAttendanceCount;

            // Model for rendering tables/cards
            var model = new AdminViewModel
            {
                PendingLeaves = _context.LeaveRequests.Where(l => l.Status == "Pending").ToList(),
                Users = _context.Users.ToList()
            };

            return View(model);
        }


        public IActionResult ForgotPass()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Features()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        [HttpGet]
        public IActionResult GetNewLeaveNotification()
        {
            var latestLeave = _context.LeaveRequests
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    name = l.FirstName + " " + l.LastName,
                    leaveType = l.LeaveType,
                    createdAt = l.CreatedAt
                })
                .FirstOrDefault();

            return Json(latestLeave);
        }

        private readonly IHubContext<LeaveHub> _hub;


        [HttpGet]
        public IActionResult GetLeaveSummary()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Unauthorized();

            var leaves = _context.LeaveRequests
                .Where(x => x.UserId == userId)
                .ToList();

            int totalLeaves = 24;

            int taken = leaves.Count(x => x.Status == "Approved");
            int pending = leaves.Count(x => x.Status == "Pending");
            int rejected = leaves.Count(x => x.Status == "Rejected");

            // 🔥 LOSS OF PAY LOGIC (MONTH-WISE)
            int lossOfPay = leaves
             .Where(x => x.Status == "Approved" && x.StartDate.HasValue)
             .GroupBy(x => new { x.StartDate.Value.Year, x.StartDate.Value.Month })
             .Sum(g =>
             {
                 int count = g.Count();
                 return count > 2 ? count - 2 : 0;
             });

            int workedDays = 365 - taken;

            return Json(new
            {
                totalLeaves,
                taken,
                pending,
                rejected,
                workedDays,
                lossOfPay
            });
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfileImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return Json(new { success = false, message = "No file" });

            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            var userId = HttpContext.Session.GetInt32("UserId");

            // 🔥 DEBUG CHECK
            if (userId == null)
            {
                return Json(new { success = false, message = "Session expired ❌" });
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId.Value);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found ❌" });
            }

            user.ProfileImage = "/images/" + fileName;

            _context.Users.Update(user);
            _context.SaveChanges();

            return Json(new { success = true, imageUrl = user.ProfileImage });
        }






    }
}
