using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AttendanceApp.Models;

namespace AttendanceApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
       
        public async Task<IActionResult> Login(LoginViewModel model,string deviceId)
        {
            if (!ModelState.IsValid)
              return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if(roles.Contains("Employee"))
                {
                    //  Fixed location (hospital)
                    //double hospitalLat = /*19.185441*/19.3559246;
                    //double hospitalLon = /*77.300809*/73.0641396;
                    //double radiusMeters = 50;

                    //if (model.Latitude == null || model.Longitude == null)
                    //{
                    //    ModelState.AddModelError(string.Empty, "📍 Location data not received. Please allow location access.");
                    //    return View(model);
                    //}

                    //var (withinRadius, distance) = IsWithinRadiusWithDistance(
                    //    hospitalLat, hospitalLon, model.Latitude.Value, model.Longitude.Value, radiusMeters
                    //);

                    //if (!withinRadius)
                    //{
                    //    string distanceStr = distance > 1000
                    //        ? $"{(distance / 1000):0.00} km"
                    //        : $"{distance:0.00} meters";

                    //    ModelState.AddModelError(string.Empty, $"❌ You are {distanceStr} away from the login zone.");
                    //    return View(model);
                    //}
                }
                // Device binding for Employee only
                if (roles.Contains("Employee"))
                {
                    //var deviceId = Request.Headers["User-Agent"].ToString();

                    if (string.IsNullOrEmpty(user.DeviceId))
                    {
                        user.DeviceId = deviceId;
                        await _userManager.UpdateAsync(user);
                    }
                    else if (user.DeviceId != deviceId)
                    {
                        ModelState.AddModelError(string.Empty, "Unauthorized device. Contact admin.");
                        return View(model);
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    Debug.WriteLine("✅ Login succeeded");

                }
                else
                {
                    Debug.WriteLine("❌ Login failed");
                }

                if (result.Succeeded)
                {
                    if (roles.Contains("Admin"))
                        return RedirectToAction("Index", "Admin");

                    else if (roles.Contains("Employee"))
                        return RedirectToAction("Index", "Employee");


                    // return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
        private (bool, double) IsWithinRadiusWithDistance(double centerLat, double centerLon, double targetLat, double targetLon, double radiusMeters)
        {
            double R = 6371000; // earth radius
            double dLat = (targetLat - centerLat) * (Math.PI / 180);
            double dLon = (targetLon - centerLon) * (Math.PI / 180);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(centerLat * (Math.PI / 180)) * Math.Cos(targetLat * (Math.PI / 180)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = R * c;

            return (distance <= radiusMeters, distance);
        }
    }
}
