using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Partner;
using SV22T1020459.Models.Security;
using SV22T1020459.Shop;
using System.Threading.Tasks;

namespace SV22T1020459.Admin.Controllers
{
    [Authorize]
    /// <summary>
    /// Quản lý tài khoản khách hàng (Đăng nhập, đăng ký, hồ sơ, đổi mật khẩu)
    /// </summary>
    public class UserController : Controller
    {
        /// <summary>
        /// Hiển thị giao diện đăng nhập
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = "/")
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return Redirect("/");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// Xử lý dữ liệu đăng nhập từ form gửi lên
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = "/")
        {
            try
            {
                ViewBag.Username = username;

                if (string.IsNullOrWhiteSpace(username))
                {
                    ModelState.AddModelError("Error", "Vui lòng nhập địa chỉ Email.");
                    return View();
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError("Error", "Vui lòng nhập Mật khẩu.");
                    return View();
                }

                string hashePassword = CryptHelper.HashMD5(password);

                var userAccount = await SecurityDataService.AuthorizeCustomerAsync(username, hashePassword);

                if (userAccount == null)
                {
                    ModelState.AddModelError("Error", "Tên đăng nhập hoặc mật khẩu không chính xác.");
                    return View();
                }
                var userData = new WebUserData
                {
                    UserId = userAccount.UserId,
                    UserName = userAccount.UserName,
                    DisplayName = userAccount.DisplayName,
                    Email = userAccount.Email,
                    Photo = userAccount.Photo,
                    Roles = userAccount.RoleNames?.Split(',').ToList() ?? new List<string>() 
                };

                var principal = userData.CreatePrincipal();
                await HttpContext.SignInAsync(principal);

                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Lỗi hệ thống: " + ex.Message);
                return View();
            }
        }

        /// <summary>
        /// Hiển thị giao diện đăng ký tài khoản
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Xử lý dữ liệu đăng ký từ form gửi lên
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(Customer data, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.Email))
                {
                    ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ Email");
                    return View(data);
                }
                if (string.IsNullOrWhiteSpace(confirmPassword))
                {
                    ModelState.AddModelError("Error", "Vui lòng nhập mật khẩu xác nhận.");
                    return View(data);
                }
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                {
                    ModelState.AddModelError("Error", "Vui lòng nhập tên khách hàng.");
                    return View(data);
                }
                if (string.IsNullOrWhiteSpace(data.Password))
                {
                    ModelState.AddModelError("Error", "Vui lòng nhập mật khẩu.");
                    return View(data);
                }

                if (data.Password.Length < 6)
                {
                    ModelState.AddModelError("", "Mật khẩu mới phải có ít nhất 6 ký tự!");
                    return View(data);
                }

                if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = data.CustomerName;
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";

                if (data.Password != confirmPassword)
                {
                    ModelState.AddModelError("Error", "Mật khẩu xác nhận không khớp.");
                    return View(data);
                }

                if (!await PartnerDataService.ValidatelCustomerEmailAsync(data.Email))
                {
                    ModelState.AddModelError("Error", "Email này đã được sử dụng. Vui lòng chọn email khác.");
                    return View(data);
                }

                data.Password = CryptHelper.HashMD5(data.Password);

                data.IsLocked = false;

                await PartnerDataService.AddCustomerAsync(data);

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login", "User");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Lỗi DB: " + ex.Message);
                // Log lỗi chi tiết ở đây nếu cần
                return View(data);
            }
        }

        /// <summary>
        /// Xóa session và đăng xuất khỏi hệ thống
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Logout()
        {

            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Hiển thị giao diện cập nhật hồ sơ cá nhân
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userIdString = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int customerId))
            {
                return RedirectToAction("Login");
            }

            var customer = await PartnerDataService.GetCustomerAsync(customerId);
            if (customer == null)
            {
                return RedirectToAction("Login");
            }
            return View(customer);
        }
        /// <summary>
        /// Xử lý cập nhật thông tin cá nhân
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(Customer data)
        {
            try
            {
                var currentUserId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
                if (currentUserId != data.CustomerID.ToString())
                    return RedirectToAction("Login");

                // ✅ Validation — trả về View với lỗi dưới từng trường
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError("CustomerName", "Vui lòng nhập tên khách hàng.");

                if (string.IsNullOrWhiteSpace(data.ContactName))
                    ModelState.AddModelError("ContactName", "Vui lòng nhập tên người liên hệ.");

                if (!ModelState.IsValid)
                    return View("Profile", data);

                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";

                bool result = await PartnerDataService.UpdateCustomerAsync(data);

                if (result)
                {
                    TempData["SuccessMessage"] = "Cập nhật hồ sơ cá nhân thành công!";

                    var currentRoles = User.Claims.FirstOrDefault(c => c.Type == "RoleNames")?.Value ?? "";
                    var userData = new WebUserData
                    {
                        UserId = data.CustomerID.ToString(),
                        UserName = data.Email,
                        DisplayName = data.CustomerName,
                        Email = data.Email,
                        Photo = "nophoto.png",
                        Roles = currentRoles.Split(',').ToList()
                    };
                    await HttpContext.SignInAsync(userData.CreatePrincipal());
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra, không thể lưu thay đổi.";
                }

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống: " + ex.Message;
                return RedirectToAction("Profile");
            }
        }

        /// <summary>
        /// Hiển thị giao diện thay đổi mật khẩu
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePassword());
        }

        /// <summary>
        /// Xử lý đổi mật khẩu
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePassword data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.oldPassword) ||
                    string.IsNullOrWhiteSpace(data.newPassword) ||
                    string.IsNullOrWhiteSpace(data.confirmPassword))
                {
                    ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ các trường mật khẩu.");
                    return View(data);
                }

                if (data.newPassword != data.confirmPassword)
                {
                    ModelState.AddModelError("Error", "Mật khẩu xác nhận không khớp với mật khẩu mới.");
                    return View(data);
                }

                if (data.oldPassword.Length < 6)
                {
                    ModelState.AddModelError("", "Mật khẩu mới phải có ít nhất 6 ký tự!");
                    return View(data);
                }

                var userData = User.GetUserData();

                if (userData == null || string.IsNullOrEmpty(userData.UserName))
                {
                    return RedirectToAction("Login");
                }

                string userName = userData.UserName;

                string hashedOldPassword = CryptHelper.HashMD5(data.oldPassword);
                var checkUser = await SecurityDataService.AuthorizeCustomerAsync(userName, hashedOldPassword);

                if (checkUser == null)
                {
                    ModelState.AddModelError("Error", "Mật khẩu hiện tại không chính xác.");
                    return View(data);
                }

                string hashedNewPassword = CryptHelper.HashMD5(data.newPassword);
                bool isSuccess = await SecurityDataService.ChangePasswordCustomerAsync(userName, hashedNewPassword);

                if (isSuccess)
                {
                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Bạn có thể sử dụng mật khẩu mới cho lần đăng nhập sau.";
                    return RedirectToAction("ChangePassword");
                }

                ModelState.AddModelError("Error", "Lỗi hệ thống: Không thể cập nhật mật khẩu lúc này.");
                return View(data);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Lỗi xử lý: " + ex.Message);
                return View(data);
            }
        }
    }
}