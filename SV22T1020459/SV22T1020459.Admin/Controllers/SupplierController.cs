using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Partner;

namespace SV22T1020459.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng quản lý dữ liệu liên quan đến nhà cung cấp
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class SupplierController : Controller
    {
        private const string SUPPLIER_SEARCH = "SupplierSearchInput";

        public IActionResult Index()
        {
            try
            {
                var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH);
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

        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            try
            {
                var result = await PartnerDataService.ListSuppliersAsync(input);
                ApplicationContext.SetSessionData(SUPPLIER_SEARCH, input);
                return View(result);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Có lỗi xảy ra khi tải dữ liệu: {ex.Message}</div>");
            }
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0,
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
                var model = await PartnerDataService.GetSupplierAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống: Không thể lấy thông tin nhà cung cấp lúc này!";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Supplier data)
        {
            try
            {
                ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật nhà cung cấp";

                // Kiểm tra Validation
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                    ModelState.AddModelError(nameof(data.SupplierName), "Tên nhà cung cấp không được để trống");

                if (string.IsNullOrWhiteSpace(data.Email))
                {
                    ModelState.AddModelError(nameof(data.Email), "Email nhà cung cấp không được để trống");
                }
                else
                {
                    bool isValidEmail = await PartnerDataService.ValidateSupplierEmailAsync(data.Email, data.SupplierID);
                    if (!isValidEmail)
                    {
                        ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng cho một nhà cung cấp khác");
                    }
                }

                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh/thành phố");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = data.SupplierName;
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";

                if (data.SupplierID == 0)
                {
                    await PartnerDataService.AddSupplierAsync(data);
                    TempData["Message"] = "Bổ sung nhà cung cấp mới thành công!";
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
                    TempData["Message"] = "Cập nhật thông tin nhà cung cấp thành công!";
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
                    await PartnerDataService.DeleteSupplierAsync(id);
                    TempData["Message"] = "Xóa nhà cung cấp thành công!";
                    return RedirectToAction("Index");
                }

                var model = await PartnerDataService.GetSupplierAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await PartnerDataService.IsUsedSupplierAsync(id);

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Không thể thực hiện xóa. Nhà cung cấp này đang cung cấp sản phẩm cho hệ thống!";
                return RedirectToAction("Index");
            }
        }
    }
}