using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Security;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020459.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng liên quan đến tài khoản người dùng (đăng nhập, đăng xuất, đổi mật khẩu)
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {

        /// <summary>
        /// Đăng nhập vào hệ thống admin
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                ViewBag.Username = username;
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu");
                    return View();
                }

                string hashePassword = CryptHelper.HashMD5(password);

                var userAccount = await SecurityDataService.AuthorizeEmployeeAsync(username, hashePassword);

                if (userAccount == null)
                {
                    ModelState.AddModelError("Error", "Tên đăng nhập hoặc mật khẩu không đúng");
                    return View();
                }

                var userData = new WebUserData
                {
                    UserId = userAccount.UserId,
                    UserName = userAccount.UserName,
                    DisplayName = userAccount.DisplayName,
                    Email = userAccount.Email,
                    Photo = userAccount.Photo,

                    Roles = string.IsNullOrEmpty(userAccount.RoleNames)
                            ? new System.Collections.Generic.List<string>()
                            : userAccount.RoleNames.Split(',').Select(r => r.Trim()).ToList()
                };

                var principal = userData.CreatePrincipal();

                await HttpContext.SignInAsync(principal);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Hệ thống đang bảo trì hoặc mất kết nối. Vui lòng thử lại sau! " + ex.Message);
                return View();
            }
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống
        /// </summary>
        public async Task<IActionResult> Logout()
        {
            try
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login");
            }
            catch
            {
                return RedirectToAction("Login");
            }
        }

        /// <summary>
        /// Đổi mật khẩu của tài khoản đang đăng nhập
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePassword());
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePassword data)
        {
            try
            {
                var userData = User.GetUserData();
                if (userData == null) return RedirectToAction("Login");

                string userName = userData.UserName;

                if (string.IsNullOrWhiteSpace(data.oldPassword) ||
                    string.IsNullOrWhiteSpace(data.newPassword) ||
                    string.IsNullOrWhiteSpace(data.confirmPassword))
                {
                    ModelState.AddModelError("", "Vui lòng nhập đầy đủ các trường thông tin!");
                    return View(data);
                }

                if (data.newPassword.Length < 6)
                {
                    ModelState.AddModelError("", "Mật khẩu mới phải có ít nhất 6 ký tự!");
                    return View(data);
                }

                if (data.newPassword == data.oldPassword)
                {
                    ModelState.AddModelError("", "Mật khẩu mới không được trùng với mật khẩu cũ!");
                    return View(data);
                }

                if (data.newPassword != data.confirmPassword)
                {
                    ModelState.AddModelError("", "Mật khẩu mới và xác nhận mật khẩu không khớp!");
                    return View(data);
                }

                string hashedOldPassword = CryptHelper.HashMD5(data.oldPassword);
                string hashedNewPassword = CryptHelper.HashMD5(data.newPassword);

                var userAccount = await SecurityDataService.AuthorizeEmployeeAsync(userName, hashedOldPassword);
                if (userAccount == null)
                {
                    ModelState.AddModelError("", "Mật khẩu cũ không chính xác!");
                    return View(data);
                }

                bool result = await SecurityDataService.ChangePasswordEmployeeAsync(userName, hashedNewPassword);

                if (result)
                {
                    ViewBag.SuccessMessage = "Đổi mật khẩu thành công!";
                    return View(new ChangePassword());
                }

                ModelState.AddModelError("", "Lỗi hệ thống, không thể đổi mật khẩu lúc này!");
                return View(data);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã có lỗi xảy ra trong quá trình xử lý: " + ex.Message);
                return View(data);
            }
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}