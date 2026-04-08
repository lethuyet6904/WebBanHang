using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Catalog;
using SV22T1020459.Models.Common;
using SV22T1020459.Admin;
using System;
using System.Threading.Tasks;

namespace SV22T1020459.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng quản lý dữ liệu liên quan đến loại hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales}")]
    public class CategoryController : Controller
    {
        private const string CATEGORY_SEARCH = "CategorySearchInput";

        /// <summary>
        /// Giao diện trang chủ quản lý loại hàng
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH);
            if (input == null)
                input = new PaginationSearchInput
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về danh sách loại hàng
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            try
            {
                var result = await CatalogDataService.ListCategoriesAsync(input);
                ApplicationContext.SetSessionData(CATEGORY_SEARCH, input);
                return View(result);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Có lỗi xảy ra khi tải dữ liệu: {ex.Message}</div>");
            }
        }

        /// <summary>
        /// Tạo mới 1 loại hàng
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Loại hàng";
            var model = new Category()
            {
                CategoryID = 0,
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật 1 loại hàng
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewBag.Title = "Cập nhật Loại hàng";
                var model = await CatalogDataService.GetCategoryAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống: Không thể lấy thông tin loại hàng lúc này!";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Lưu dữ liệu (Thêm mới hoặc Cập nhật)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            try
            {
                ViewBag.Title = data.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật loại hàng";

                if (string.IsNullOrWhiteSpace(data.CategoryName))
                    ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");

                if (string.IsNullOrWhiteSpace(data.Description))
                    ModelState.AddModelError(nameof(data.Description), "Vui lòng nhập mô tả loại hàng");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                if (data.CategoryID == 0)
                {
                    await CatalogDataService.AddCategoryAsync(data);
                    TempData["Message"] = "Bổ sung loại hàng mới thành công!";
                }
                else
                {
                    await CatalogDataService.UpdateCategoryAsync(data);
                    TempData["Message"] = "Cập nhật thông tin loại hàng thành công!";
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
        /// Xóa 1 loại hàng
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await CatalogDataService.DeleteCategoryAsync(id);
                    TempData["Message"] = "Xóa loại hàng thành công!";
                    return RedirectToAction("Index");
                }

                var model = await CatalogDataService.GetCategoryAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await CatalogDataService.IsUsedCategoryAsync(id);

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Không thể thực hiện xóa. Loại hàng này có thể đang ràng buộc với mặt hàng nào đó!";
                return RedirectToAction("Index");
            }
        }
    }
}