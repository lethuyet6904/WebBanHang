using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.HR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SV22T1020459.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng quản lý dữ liệu liên quan đến nhân viên
    /// </summary>
    [Authorize(Roles = WebUserRoles.Administrator)]
    public class EmployeeController : Controller
    {
        private const string EMPLOYEE_SEARCH = "EmployeeSearchInput";

        /// <summary>
        /// Hiển thị giao diện danh sách nhân viên
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH);
                if (input == null)
                    input = new PaginationSearchInput
                    {
                        Page = 1,
                        PageSize = ApplicationContext.PageSize,
                        SearchValue = ""
                    };

                return View(input);
            }
            catch
            {
                return View(new PaginationSearchInput { Page = 1, PageSize = ApplicationContext.PageSize, SearchValue = "" });
            }
        }

        /// <summary>
        /// Tìm kiếm
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            try
            {
                var result = await HRDataService.ListEmployeesAsync(input);
                ApplicationContext.SetSessionData(EMPLOYEE_SEARCH, input);
                return View(result);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Có lỗi xảy ra khi tải dữ liệu: {ex.Message}</div>");
            }
        }

        /// <summary>
        /// Bổ sung 1 nhân viên mới
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewBag.Title = "Cập nhật thông tin nhân viên";
                var model = await HRDataService.GetEmployeeAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống: Không thể lấy thông tin nhân viên lúc này!";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                if (data.EmployeeID == 0)
                {
                    await HRDataService.AddEmployeeAsync(data);
                    TempData["Message"] = "Bổ sung nhân viên mới thành công!";
                }
                else
                {
                    await HRDataService.UpdateEmployeeAsync(data);
                    TempData["Message"] = "Cập nhật thông tin nhân viên thành công!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Hệ thống đang bận hoặc dữ liệu không hợp lệ. ({ex.Message})");
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xoá 1 nhân viên
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await HRDataService.DeleteEmployeeAsync(id);
                    TempData["Message"] = "Xóa nhân viên thành công!";
                    return RedirectToAction("Index");
                }

                var model = await HRDataService.GetEmployeeAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await HRDataService.IsUsedEmployeeAsync(id);

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Không thể thực hiện xóa. Nhân viên này có thể đang ràng buộc dữ liệu đơn hàng!";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = WebUserRoles.Administrator)]
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            try
            {
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                    return RedirectToAction("Index");

                return View(employee);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống khi tải thông tin mật khẩu.";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = WebUserRoles.Administrator)]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            try
            {
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                    return RedirectToAction("Index");

                if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    ModelState.AddModelError("", "Vui lòng nhập đầy đủ mật khẩu mới và xác nhận!");
                    return View(employee);
                }

                if (newPassword.Length < 6)
                {
                    ModelState.AddModelError("", "Mật khẩu mới phải có ít nhất 6 ký tự!");
                    return View(employee);
                }

                if (newPassword != confirmPassword)
                {
                    ModelState.AddModelError("", "Mật khẩu xác nhận không trùng khớp!");
                    return View(employee);
                }

                string hashedNewPassword = CryptHelper.HashMD5(newPassword);
                bool result = await SecurityDataService.ChangePasswordEmployeeAsync(employee.Email, hashedNewPassword);

                if (result)
                {
                    TempData["Message"] = "Đổi mật khẩu cho nhân viên thành công!";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Lỗi hệ thống, không thể đổi mật khẩu lúc này!");
                return View(employee);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi nghiêm trọng trong quá trình đổi mật khẩu! " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = WebUserRoles.Administrator)]
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            try
            {
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                    return RedirectToAction("Index");

                return View(employee);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống khi tải thông tin phân quyền.";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = WebUserRoles.Administrator)]
        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, List<string> selectedRoles)
        {
            try
            {
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                    return RedirectToAction("Index");

                string roleNames = selectedRoles != null ? string.Join(",", selectedRoles) : "";
                bool result = await HRDataService.UpdateEmployeeRoleAsync(id, roleNames);

                if (result)
                {
                    TempData["Message"] = "Cập nhật phân quyền thành công!";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Lỗi hệ thống, không thể phân quyền lúc này!");
                return View(employee);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi khi lưu quyền hạn: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}