using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Leave_system.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Diagnostics;

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

            

            // Insert via stored procedure
            string sql = "EXEC sp_InsertUser @FirstName, @LastName, @Phone, @Email, @Password";
            _context.Database.ExecuteSqlRaw(
                sql,
                new SqlParameter("@FirstName", user.FirstName),
                new SqlParameter("@LastName", user.LastName),
                new SqlParameter("@Phone", user.Phone),
                new SqlParameter("@Email", user.Email),
                new SqlParameter("@Password", user.Password)
            );

            return RedirectToAction("Login");
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
                var hasher = new PasswordHasher<User>();
                var result = hasher.VerifyHashedPassword(user, user.Password, password);

                if (result == PasswordVerificationResult.Success)
                {
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserId", user.Id.ToString());

                    return RedirectToAction("User"); 
                }
            }

            ViewBag.Error = "Invalid email or password";
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
            return View();
        }

        public IActionResult Admin()
        {
            return View();
        }

        public IActionResult ForgotPass()
        {
            return View();
        }

        public IActionResult Privacy()
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
