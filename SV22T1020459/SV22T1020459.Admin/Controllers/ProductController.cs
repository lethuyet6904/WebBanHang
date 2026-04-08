using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Catalog;

namespace SV22T1020459.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng quản lý dữ liệu liên quan đến mặt hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales}")]
    public class ProductController : Controller
    {
        private const string PRODUCT_SEARCH = "ProductSearchInput";

        /// <summary>
        /// Tìm kiếm, hiển thị danh sách mặt hàng
        /// </summary>
        public IActionResult Index()
        {
            try
            {
                var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
                if (input == null)
                    input = new ProductSearchInput
                    {
                        Page = 1,
                        PageSize = ApplicationContext.PageSize,
                        SearchValue = "",
                        CategoryID = 0,
                        SupplierID = 0,
                        MinPrice = 0,
                        MaxPrice = 0
                    };
                return View(input);
            }
            catch
            {
                return View(new ProductSearchInput { Page = 1, PageSize = ApplicationContext.PageSize, SearchValue = "" });
            }
        }

        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            try
            {
                var result = await CatalogDataService.ListProductsAsync(input);
                ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
                return View(result);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Có lỗi xảy ra khi tải dữ liệu: {ex.Message}</div>");
            }
        }

        /// <summary>
        /// Tạo mới một mặt hàng
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung sản phẩm";
            var model = new Product()
            {
                ProductID = 0,
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật 1 mặt hàng
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewBag.Title = "Cập nhật sản phẩm";
                var model = await CatalogDataService.GetProductAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
                ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

                ViewBag.ProductID = id;
                return View(model);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống: Không thể lấy thông tin sản phẩm lúc này!";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung sản phẩm" : "Cập nhật sản phẩm";

                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Tên sản phẩm không được để trống");

                if (data.CategoryID == 0)
                    ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");

                if (data.SupplierID == 0)
                    ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");

                if (!ModelState.IsValid)
                {
                    ViewBag.Photos = await CatalogDataService.ListPhotosAsync(data.ProductID);
                    ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(data.ProductID);
                    return View("Edit", data);
                }

                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/products", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";
                if (string.IsNullOrEmpty(data.ProductDescription)) data.ProductDescription = "";

                if (data.ProductID == 0)
                {
                    await CatalogDataService.AddProductAsync(data);
                    TempData["Message"] = "Thêm sản phẩm mới thành công!";
                }
                else
                {
                    await CatalogDataService.UpdateProductAsync(data);
                    TempData["Message"] = "Cập nhật sản phẩm thành công!";
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
        /// Xoá 1 mặt hàng
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await CatalogDataService.DeleteProductAsync(id);
                    TempData["Message"] = "Xóa sản phẩm thành công!";
                    return RedirectToAction("Index");
                }

                var model = await CatalogDataService.GetProductAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await CatalogDataService.IsUsedProductAsync(id);

                return View(model);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống: Không thể thực hiện xóa sản phẩm!";
                return RedirectToAction("Index");
            }
        }

        #region XỬ LÝ THUỘC TÍNH (ATTRIBUTE)

        public async Task<IActionResult> ListAttributes(int id)
        {
            try
            {
                var result = await CatalogDataService.ListAttributesAsync(id);
                return View(result);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Lỗi: {ex.Message}</div>");
            }
        }

        public IActionResult CreateAttribute(int id)
        {
            ViewBag.Title = "Bổ sung thuộc tính";
            var model = new ProductAttribute()
            {
                AttributeID = 0,
                ProductID = id,
                DisplayOrder = 1
            };
            ViewBag.ProductId = id;
            return View("EditAttribute", model);
        }

        public async Task<IActionResult> EditAttribute(int id, int attributeId)
        {
            try
            {
                ViewBag.Title = "Cập nhật thuộc tính";
                var model = await CatalogDataService.GetAttributeAsync(attributeId);
                if (model == null)
                    return RedirectToAction("Edit", new { id = id });

                ViewBag.ProductId = id;
                return View("EditAttribute", model);
            }
            catch
            {
                TempData["Error"] = "Lỗi khi lấy thông tin thuộc tính!";
                return RedirectToAction("Edit", new { id = id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveDataAttribute(ProductAttribute data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.AttributeName))
                    ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được để trống");

                if (string.IsNullOrWhiteSpace(data.AttributeValue))
                    ModelState.AddModelError(nameof(data.AttributeValue), "Giá trị thuộc tính không được để trống");

                if (data.DisplayOrder <= 0)
                    ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị phải là số nguyên dương");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Cập nhật thuộc tính";
                    ViewBag.ProductId = data.ProductID;
                    return View("EditAttribute", data);
                }

                if (data.AttributeID == 0)
                {
                    await CatalogDataService.AddAttributeAsync(data);
                    TempData["Message"] = "Bổ sung thuộc tính thành công!";
                }
                else
                {
                    await CatalogDataService.UpdateAttributeAsync(data);
                    TempData["Message"] = "Cập nhật thuộc tính thành công!";
                }

                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận. Vui lòng thử lại sau.");
                ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Cập nhật thuộc tính";
                ViewBag.ProductId = data.ProductID;
                return View("EditAttribute", data);
            }
        }

        public async Task<IActionResult> DeleteAttribute(int id, int attributeId)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await CatalogDataService.DeleteAttributeAsync(attributeId);
                    TempData["Message"] = "Xóa thuộc tính thành công!";
                    return RedirectToAction("Edit", new { id = id });
                }

                var model = await CatalogDataService.GetAttributeAsync(attributeId);
                if (model == null)
                    return RedirectToAction("Edit", new { id = id });

                ViewBag.ProductID = id;
                return View(model);
            }
            catch
            {
                TempData["Error"] = "Lỗi khi xóa thuộc tính!";
                return RedirectToAction("Edit", new { id = id });
            }
        }

        #endregion

        #region XỬ LÝ HÌNH ẢNH (PHOTO)

        public async Task<IActionResult> ListPhotos(int id)
        {
            try
            {
                var data = await CatalogDataService.ListPhotosAsync(id);
                ViewBag.ProductID = id;
                return View(data);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Lỗi: {ex.Message}</div>");
            }
        }

        public IActionResult CreatePhoto(int id)
        {
            ViewBag.Title = "Bổ sung ảnh mặt hàng";
            var model = new ProductPhoto()
            {
                PhotoID = 0,
                ProductID = id,
                IsHidden = false
            };
            ViewBag.ProductId = id;
            return View("EditPhoto", model);
        }

        public async Task<IActionResult> EditPhoto(int id, int photoId)
        {
            try
            {
                ViewBag.Title = "Cập nhật ảnh mặt hàng";
                var model = await CatalogDataService.GetPhotoAsync(photoId);
                if (model == null)
                    return RedirectToAction("Edit", new { id = id });

                ViewBag.ProductId = id;
                return View(model);
            }
            catch
            {
                TempData["Error"] = "Lỗi khi lấy thông tin hình ảnh!";
                return RedirectToAction("Edit", new { id = id });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveDataPhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            try
            {
                if (data.DisplayOrder <= 0)
                    ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị phải là số nguyên dương");

                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/products", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }
                else if (data.PhotoID == 0)
                {
                    ModelState.AddModelError(nameof(data.Photo), "Vui lòng chọn file ảnh");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.PhotoID == 0 ? "Bổ sung ảnh mặt hàng" : "Cập nhật ảnh mặt hàng";
                    ViewBag.ProductId = data.ProductID;
                    return View("EditPhoto", data);
                }

                if (data.PhotoID == 0)
                {
                    await CatalogDataService.AddPhotoAsync(data);
                    TempData["Message"] = "Bổ sung hình ảnh thành công!";
                }
                else
                {
                    await CatalogDataService.UpdatePhotoAsync(data);
                    TempData["Message"] = "Cập nhật hình ảnh thành công!";
                }

                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận, vui lòng thử lại sau.");
                ViewBag.ProductId = data.ProductID;
                return View("EditPhoto", data);
            }
        }

        public async Task<IActionResult> DeletePhoto(int id, long photoId)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await CatalogDataService.DeletePhotoAsync(photoId);
                    TempData["Message"] = "Xóa hình ảnh thành công!";
                    return RedirectToAction("Edit", new { id = id });
                }

                var model = await CatalogDataService.GetPhotoAsync(photoId);
                if (model == null)
                    return RedirectToAction("Edit", new { id = id });

                ViewBag.ProductID = id;
                return View(model);
            }
            catch
            {
                TempData["Error"] = "Lỗi khi xóa hình ảnh!";
                return RedirectToAction("Edit", new { id = id });
            }
        }

        #endregion
    }
}