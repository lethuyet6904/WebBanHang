using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Catalog;
using SV22T1020459.Shop;
using System.Threading.Tasks;

namespace SV22T1020459.Admin.Controllers
{
    /// <summary>
    /// Quản lý hiển thị và tìm kiếm các mặt hàng cho khách mua
    /// </summary>
    public class ProductController : Controller
    {
        private const string PRODUCT_SEARCH = "ProductSearchInput";
        public IActionResult Index()
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

        /// <summary>
        /// Tìm kiếm phân trang và lọc mặt hàng (Theo từ khóa, loại hàng, khoảng giá)
        /// Đã chuyển từ HomeController sang đây.
        /// </summary>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Xem chi tiết của một sản phẩm (hiển thị mô tả, thuộc tính, ảnh)
        /// </summary>
        /// <param name="id">Mã sản phẩm</param>
        public async Task<IActionResult> Detail(int id)
        {
            if (id <= 0)
                return RedirectToAction("Index");

            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id) ?? new List<ProductAttribute>();
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id) ?? new List<ProductPhoto>();

            var category = product.CategoryID.HasValue
                ? await CatalogDataService.GetCategoryAsync(product.CategoryID.Value)
                : null;
            ViewBag.Category = category?.CategoryName ?? "Không xác định";
            int categoryId = category?.CategoryID ?? 0;

            var supplier = product.SupplierID.HasValue
                ? await PartnerDataService.GetSupplierAsync(product.SupplierID.Value)
                : null;
            ViewBag.Supplier = supplier?.SupplierName ?? "Không xác định";

            var relatedInput = new ProductSearchInput
            {
                Page = 1,
                PageSize = 5,
                CategoryID = categoryId
            };
            var relatedResult = categoryId > 0
                ? await CatalogDataService.ListProductsAsync(relatedInput)
                : null;
            ViewBag.RelatedProducts = relatedResult?.DataItems
                                          .Where(p => p.ProductID != id)
                                          .Take(4)
                                          .ToList()
                                      ?? new List<Product>();

            return View(product);
        }
    }
}