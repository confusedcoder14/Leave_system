using Leave_system.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;


namespace Leave_system.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
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
            if (user != null) 
            {
                if (user.Password == password) 
                {
                    HttpContext.Session.SetString("UserEmail", user.Email); 
                    /*HttpContext.Session.SetString("UserId", user.Id.ToString());*/ 
                    return RedirectToAction("User"); 
                }
            } ViewBag.Error = "Invalid email or password"; 
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }


        public IActionResult AdminLogin()
        {
            return View();
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
        public IActionResult ApplyLeave([FromForm] LeaveRequest leave)
        {
            if (leave == null)
                return Json(new { success = false, message = "Form binding failed." });

            leave.Status = "Pending";
            leave.CreatedAt = DateTime.Now;

            try
            {
                _context.LeaveRequests.Add(leave);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = true });
        }




        /* public IActionResult Admin()
          {
              var users = _context.Users.ToList();
              return View(users);
          }*/

        public IActionResult Admin()
        {
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
        public IActionResult UpdateLeaveStatus([FromBody] LeaveStatusVM model)
        {
            var leave = _context.LeaveRequests.FirstOrDefault(l => l.Id == model.Id);
            if (leave == null)
                return Json(new { success = false });

            leave.Status = model.Status;
            _context.SaveChanges();

            return Json(new { success = true });
        }


        [HttpGet]
        public IActionResult GetReports()
        {
            var data = _context.LeaveRequests
                .Where(l => l.Status == "Approved" || l.Status == "Rejected")
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new {
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
    }
}
