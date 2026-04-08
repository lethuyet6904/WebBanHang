using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Partner;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SV22T1020459.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng quản lý dữ liệu liên quan đến khách hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CustomerController : Controller
    {
        private const string CUSTOMER_SEARCH = "CustomerSearchInput";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH);
            if (input == null)
                input = new PaginationSearchInput
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };

            return View(input);
        }

        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            try
            {
                var result = await PartnerDataService.ListCustomersAsync(input);
                ApplicationContext.SetSessionData(CUSTOMER_SEARCH, input);
                return View(result);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Có lỗi xảy ra khi tải dữ liệu: {ex.Message}</div>");
            }
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Khách hàng";
            var model = new Customer()
            {
                CustomerID = 0,
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewBag.Title = "Cập nhật Khách hàng";
                var model = await PartnerDataService.GetCustomerAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống: Không thể lấy thông tin khách hàng lúc này!";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            try
            {
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật khách hàng";

                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Email khách hàng không được để trống");
                else if (!await PartnerDataService.ValidatelCustomerEmailAsync(data.Email, data.CustomerID))
                    ModelState.AddModelError(nameof(data.Email), "Email khách hàng đã tồn tại");

                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh/thành");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = data.CustomerName;
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";

                if (data.CustomerID == 0)
                {
                    await PartnerDataService.AddCustomerAsync(data);
                    TempData["Message"] = "Bổ sung khách hàng mới thành công!";
                }
                else
                {
                    await PartnerDataService.UpdateCustomerAsync(data);
                    TempData["Message"] = "Cập nhật thông tin khách hàng thành công!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng thử lại sau.");
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await PartnerDataService.DeleteCustomerAsync(id);
                    TempData["Message"] = "Xóa khách hàng thành công!";
                    return RedirectToAction("Index");
                }

                var model = await PartnerDataService.GetCustomerAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await PartnerDataService.IsUsedCustomerAsync(id);
                return View(model);
            }
            catch
            {
                TempData["Error"] = "Không thể thực hiện xóa. Khách hàng này có thể đang ràng buộc dữ liệu khác!";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            try
            {
                var customer = await PartnerDataService.GetCustomerAsync(id);
                if (customer == null)
                    return RedirectToAction("Index");

                return View(customer);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống khi tải thông tin mật khẩu.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            try
            {
                var customer = await PartnerDataService.GetCustomerAsync(id);
                if (customer == null)
                    return RedirectToAction("Index");

                if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                {
                    ModelState.AddModelError("", "Vui lòng nhập mật khẩu mới và xác nhận mật khẩu!");
                    return View(customer);
                }

                if (newPassword.Length < 6)
                {
                    ModelState.AddModelError("", "Mật khẩu mới phải có ít nhất 6 ký tự!");
                    return View(customer);
                }

                if (newPassword != confirmPassword)
                {
                    ModelState.AddModelError("", "Mật khẩu xác nhận không trùng khớp!");
                    return View(customer);
                }

                string hashedNewPassword = CryptHelper.HashMD5(newPassword);
                bool result = await SecurityDataService.ChangePasswordCustomerAsync(customer.Email, hashedNewPassword);

                if (result)
                {
                    TempData["Message"] = $"Đổi mật khẩu cho khách hàng {customer.CustomerName} thành công!";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Lỗi hệ thống, không thể đổi mật khẩu lúc này!");
                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi nghiêm trọng trong quá trình đổi mật khẩu!";
                return RedirectToAction("Index");
            }
        }
    }
}