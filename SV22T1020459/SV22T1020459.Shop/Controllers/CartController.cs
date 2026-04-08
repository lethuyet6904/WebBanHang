using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Sales;
using SV22T1020459.Shop;
using System.Threading.Tasks;

namespace SV22T1020459.Admin.Controllers
{
    /// <summary>
    /// Quản lý giỏ hàng lưu trữ trong Session của khách hàng
    /// </summary>
    [Authorize]
    public class CartController : Controller
    {
        /// <summary>
        /// Hiển thị giao diện danh sách các món hàng trong giỏ
        /// </summary>
        public IActionResult Index()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return View(cart);
        }

        /// <summary>
        /// Thêm mặt hàng vào giỏ hàng.
        /// - Nếu là AJAX/fetch request → trả về JSON { ok, message, cartCount }
        /// - Nếu là form submit thường (Sec-Fetch-Mode: navigate) → redirect như cũ
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            // Fetch API gửi Sec-Fetch-Mode: "cors" hoặc "same-origin", còn
            // browser navigation gửi Sec-Fetch-Mode: "navigate"
            string secFetchMode = Request.Headers["Sec-Fetch-Mode"].ToString();
            bool isFetchRequest = !string.IsNullOrEmpty(secFetchMode) && secFetchMode != "navigate";

            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null || !product.IsSelling)
            {
                if (isFetchRequest)
                    return Json(new { ok = false, message = "Sản phẩm không tồn tại hoặc đã ngừng bán!" });

                TempData["Error"] = "Sản phẩm không tồn tại hoặc đã ngừng bán!";
                return RedirectToAction("Index", "Home");
            }

            var item = new OrderDetailViewInfo
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                Photo = product.Photo ?? "nophoto.png",
                SalePrice = product.Price,
                Quantity = quantity,
                Unit = product.Unit
            };

            ShoppingCartService.AddCartItem(item);

            int cartCount = ShoppingCartService.GetShoppingCart().Count;

            if (isFetchRequest)
            {
                return Json(new
                {
                    ok = true,
                    message = $"Đã thêm {product.ProductName} vào giỏ hàng!",
                    cartCount = cartCount
                });
            }

            TempData["SuccessMessage"] = $"Đã thêm {product.ProductName} vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Cập nhật số lượng của một mặt hàng trong giỏ
        /// </summary>
        [HttpPost]
        public IActionResult Update(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                ShoppingCartService.RemoveCartItem(productId);
            }
            else
            {
                var item = ShoppingCartService.GetCartItem(productId);
                if (item != null)
                {
                    ShoppingCartService.UpdateCartItem(productId, quantity, item.SalePrice);
                }
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xoá 1 mặt hàng khỏi giỏ hàng (Dùng HttpGet vì gọi từ thẻ <a>)
        /// </summary>
        [HttpGet]
        public IActionResult Remove(int id)
        {
            ShoppingCartService.RemoveCartItem(id);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng (Dùng HttpGet vì gọi từ thẻ <a>)
        /// </summary>
        [HttpGet]
        public IActionResult Clear()
        {
            ShoppingCartService.ClearCart();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Mua ngay: them vao gio roi chuyen thang den Checkout
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> BuyNow(int ProductID, int Quantity)
        {
            if (Quantity <= 0) Quantity = 1;

            var product = await CatalogDataService.GetProductAsync(ProductID);
            if (product == null || !product.IsSelling)
            {
                TempData["Error"] = "Sản phẩm không tồn tại hoặc đã ngừng bán!";
                return RedirectToAction("Index", "Home");
            }

            var item = new OrderDetailViewInfo
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                Photo = product.Photo ?? "nophoto.png",
                SalePrice = product.Price,
                Quantity = Quantity,
                Unit = product.Unit
            };

            ShoppingCartService.AddCartItem(item);

            return RedirectToAction("Checkout", "Order", new { productIds = new[] { ProductID } });
        }
    }
}