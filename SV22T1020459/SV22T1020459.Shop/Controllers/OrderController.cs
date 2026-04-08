using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Sales;
using SV22T1020459.Shop;

namespace SV22T1020459.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        [HttpGet]
        public IActionResult Checkout([FromQuery] int[] productIds)
        {
            var cart = ShoppingCartService.GetShoppingCart();
            var selectedItems = cart.Where(m => productIds.Contains(m.ProductID)).ToList();

            if (selectedItems.Count == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            return View(selectedItems);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(
            string DeliveryProvince,
            string DeliveryAddress,
            int[] productIds)
        {
            var cart = ShoppingCartService.GetShoppingCart();
            var selectedItems = cart.Where(m => productIds.Contains(m.ProductID)).ToList();

            if (selectedItems.Count == 0)
                return RedirectToAction("Index", "Cart");

            if (string.IsNullOrWhiteSpace(DeliveryProvince) || string.IsNullOrWhiteSpace(DeliveryAddress))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ địa chỉ giao hàng.";
                return RedirectToAction("Checkout", new { productIds });
            }

            var userData = User.GetUserData();
            int customerId = 0;
            if (userData != null && int.TryParse(userData.UserId, out int parsed))
                customerId = parsed;

            int orderID = await SalesDataService.AddOrderAsync(customerId, DeliveryProvince, DeliveryAddress);

            if (orderID > 0)
            {
                foreach (var item in selectedItems)
                {
                    var detail = new OrderDetail
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    };
                    await SalesDataService.AddDetailAsync(detail);
                    ShoppingCartService.RemoveCartItem(item.ProductID);
                }

                TempData["SuccessMessage"] = "Đặt hàng thành công! Chúng tôi sẽ liên hệ xác nhận sớm nhất.";
                return RedirectToAction("History");
            }

            TempData["Error"] = "Có lỗi xảy ra khi tạo đơn hàng. Vui lòng thử lại.";
            return RedirectToAction("Checkout", new { productIds });
        }

        /// <summary>
        /// Lịch sử đơn hàng — lọc theo trạng thái và phân trang
        /// status = 0: tất cả, 1: Chờ duyệt, 2: Đã duyệt, 3: Đang giao, 4: Hoàn tất, -1: Đã hủy, -2: Bị từ chối
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> History(int status = 0, int page = 1)
        {
            var userData = User.GetUserData();
            int customerId = 0;
            if (userData != null && int.TryParse(userData.UserId, out int parsed))
                customerId = parsed;

            if (customerId == 0)
                return RedirectToAction("Login", "User");

            var input = new OrderSearchInput
            {
                CustomerID = customerId,
                Status = (OrderStatusEnum)status,
                Page = page < 1 ? 1 : page,
                PageSize = 10,
                SearchValue = ""
            };

            var data = await SalesDataService.ListOrdersAsync(input);
            ViewBag.CurrentStatus = status;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("History");

            var userData = User.GetUserData();
            if (userData == null || order.CustomerID?.ToString() != userData.UserId)
            {
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này!";
                return RedirectToAction("History");
            }

            ViewBag.OrderDetails = await SalesDataService.ListDetailsAsync(id);
            return View(order);
        }

        /// <summary>
        /// Huỷ đơn hàng — chỉ được huỷ khi đang ở trạng thái Chờ duyệt
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("History");

            var userData = User.GetUserData();
            if (userData == null || order.CustomerID?.ToString() != userData.UserId)
            {
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này!";
                return RedirectToAction("History");
            }

            if (order.Status != OrderStatusEnum.New)
            {
                TempData["Error"] = "Chỉ có thể huỷ đơn hàng đang ở trạng thái Chờ duyệt.";
                return RedirectToAction("Detail", new { id });
            }

            bool ok = await SalesDataService.CancelOrderAsync(id);
            TempData[ok ? "SuccessMessage" : "Error"] = ok
                ? "Đã huỷ đơn hàng thành công."
                : "Có lỗi khi huỷ đơn hàng. Vui lòng thử lại.";

            return RedirectToAction("Detail", new { id });
        }
    }
}
