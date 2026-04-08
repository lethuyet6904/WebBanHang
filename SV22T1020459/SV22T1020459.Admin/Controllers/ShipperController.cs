using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Partner;

namespace SV22T1020459.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng quản lý dữ liệu liên quan đến shipper (người giao hàng)
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ShipperController : Controller
    {
        private const string SHIPPER_SEARCH = "ShipperSearchInput";

        /// <summary>
        /// Hiển thị giao diện danh sách shipper
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH);
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
        /// Tìm kiếm và trả về danh sách shipper
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            try
            {
                var result = await PartnerDataService.ListShippersAsync(input);
                ApplicationContext.SetSessionData(SHIPPER_SEARCH, input);
                return View(result);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Có lỗi xảy ra khi tải dữ liệu: {ex.Message}</div>");
            }
        }

        /// <summary>
        /// Bổ sung 1 shipper mới
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Shipper";
            var model = new Shipper()
            {
                ShipperID = 0,
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật 1 shipper
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewBag.Title = "Cập nhật Shipper";
                var model = await PartnerDataService.GetShipperAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống: Không thể lấy thông tin người giao hàng lúc này!";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Lưu dữ liệu (Thêm mới hoặc Cập nhật)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveData(Shipper data)
        {
            try
            {
                ViewBag.Title = data.ShipperID == 0 ? "Bổ sung Shipper" : "Cập nhật Shipper";

                if (string.IsNullOrWhiteSpace(data.ShipperName))
                    ModelState.AddModelError(nameof(data.ShipperName), "Tên người giao hàng không được để trống");

                if (string.IsNullOrWhiteSpace(data.Phone))
                    ModelState.AddModelError(nameof(data.Phone), "Vui lòng nhập số điện thoại");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (data.ShipperID == 0)
                {
                    await PartnerDataService.AddShipperAsync(data);
                    TempData["Message"] = "Bổ sung người giao hàng mới thành công!";
                }
                else
                {
                    await PartnerDataService.UpdateShipperAsync(data);
                    TempData["Message"] = "Cập nhật thông tin người giao hàng thành công!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng thử lại sau.");
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa 1 shipper
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await PartnerDataService.DeleteShipperAsync(id);
                    TempData["Message"] = "Xóa người giao hàng thành công!";
                    return RedirectToAction("Index");
                }

                var model = await PartnerDataService.GetShipperAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await PartnerDataService.IsUsedShipperAsync(id);

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Không thể thực hiện xóa. Người giao hàng này có thể đang ràng buộc với đơn hàng!";
                return RedirectToAction("Index");
            }
        }
    }
}