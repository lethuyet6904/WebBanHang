using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models.Catalog;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Sales;
using SV22T1020459.Admin;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020459.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng quản lý dữ liệu liên quan đến đơn hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales}")]
    public class OrderController : Controller
    {
        private const string ORDER_SEARCH = "OrderSearchInput";
        private const string PRODUCT_SEARCH = "SearchSellProduct";

        public IActionResult Index()
        {
            try
            {
                var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH);
                if (input == null)
                    input = new OrderSearchInput
                    {
                        Page = 1,
                        PageSize = ApplicationContext.PageSize,
                        SearchValue = "",
                        Status = 0,
                        DateFrom = null,
                        DateTo = null,
                    };
                return View(input);
            }
            catch
            {
                return View(new OrderSearchInput { Page = 1, PageSize = ApplicationContext.PageSize, SearchValue = "", Status = 0 });
            }
        }

        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            try
            {
                var result = await SalesDataService.ListOrdersAsync(input);
                ApplicationContext.SetSessionData(ORDER_SEARCH, input);
                return View(result);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Có lỗi xảy ra khi tải dữ liệu: {ex.Message}</div>");
            }
        }

        public IActionResult Create()
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
                    };
                return View(input);
            }
            catch
            {
                return View(new ProductSearchInput { Page = 1, PageSize = ApplicationContext.PageSize, SearchValue = "" });
            }
        }

        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            try
            {
                var result = await CatalogDataService.ListProductsAsync(input);
                ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
                return View(result);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Có lỗi xảy ra: {ex.Message}</div>");
            }
        }

        public IActionResult ShowCart()
        {
            try
            {
                var cart = ShoppingCartService.GetShoppingCart();
                return View(cart);
            }
            catch
            {
                return Content("<div class='alert alert-danger'>Lỗi giỏ hàng!</div>");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productId, int quantity, decimal price)
        {
            try
            {
                if (quantity <= 0)
                    return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));
                if (price < 0)
                    return Json(new ApiResult(0, "Giá bán không hợp lệ!"));

                var product = await CatalogDataService.GetProductAsync(productId);

                if (product == null)
                    return Json(new ApiResult(0, "Sản phẩm không tồn tại!"));
                if (!product.IsSelling)
                    return Json(new ApiResult(0, "Sản phẩm không còn bán!"));

                ShoppingCartService.AddCartItem(new OrderDetailViewInfo()
                {
                    ProductID = productId,
                    ProductName = product.ProductName,
                    Quantity = quantity,
                    SalePrice = price,
                    Unit = product.Unit,
                    Photo = product.Photo ?? "nophoto.png"
                });
                return Json(new ApiResult(1));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, "Lỗi hệ thống: " + ex.Message));
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await SalesDataService.DeleteOrderAsync(id);
                    TempData["Message"] = "Xóa đơn hàng thành công!";
                    return RedirectToAction("Index");
                }

                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null) return RedirectToAction("Index");

                return PartialView(order);
            }
            catch
            {
                TempData["Error"] = "Lỗi hệ thống: Không thể xóa đơn hàng lúc này!";
                return RedirectToAction("Index");
            }
        }

        public IActionResult EditCartItem(int productId = 0)
        {
            try
            {
                var item = ShoppingCartService.GetCartItem(productId);
                return PartialView(item);
            }
            catch
            {
                return Content("<div class='alert alert-danger'>Lỗi không thể tải dữ liệu!</div>");
            }
        }

        [HttpPost]
        public IActionResult UpdateCartItem(int productId, int quantity, decimal salePrice)
        {
            try
            {
                if (quantity <= 0)
                    return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));
                if (salePrice < 0)
                    return Json(new ApiResult(0, "Giá bán không hợp lệ!"));

                ShoppingCartService.UpdateCartItem(productId, quantity, salePrice);
                return Json(new ApiResult(1));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, "Lỗi hệ thống: " + ex.Message));
            }
        }

        public IActionResult DeleteCartItem(int productId = 0)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    ShoppingCartService.RemoveCartItem(productId);
                    return Json(new ApiResult(1));
                }
                var item = ShoppingCartService.GetCartItem(productId);
                return PartialView(item);
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, "Lỗi: " + ex.Message));
            }
        }

        public IActionResult ClearCart()
        {
            try
            {
                if (Request.Method == "POST")
                {
                    ShoppingCartService.ClearCart();
                    return Json(new ApiResult(1));
                }
                return PartialView();
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, "Lỗi: " + ex.Message));
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            try
            {
                var cart = ShoppingCartService.GetShoppingCart();
                if (cart.Count == 0)
                    return Json(new ApiResult(0, "Giỏ hàng trống!"));

                if (customerID <= 0)
                    return Json(new ApiResult(0, "Khách hàng không hợp lệ!"));

                int orderID = await SalesDataService.AddOrderAsync(customerID, province, address);

                foreach (var item in cart)
                {
                    await SalesDataService.AddDetailAsync(new OrderDetail()
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    });
                }

                ShoppingCartService.ClearCart();
                return Json(new ApiResult(orderID));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, "Lỗi tạo đơn hàng: " + ex.Message));
            }
        }


        #region XỬ LÝ TRẠNG THÁI ĐƠN HÀNG

        public async Task<IActionResult> Accept(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    var userData = User.GetUserData();
                    if (userData == null || string.IsNullOrEmpty(userData.UserId))
                    {
                        TempData["Error"] = "Phiên đăng nhập không hợp lệ!";
                        return RedirectToAction("Detail", new { id = id });
                    }

                    int employeeID = Convert.ToInt32(userData.UserId);
                    await SalesDataService.AcceptOrderAsync(id, employeeID);
                    TempData["Message"] = "Đã duyệt và tiếp nhận đơn hàng!";
                    return RedirectToAction("Detail", new { id = id });
                }

                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null) return RedirectToAction("Index");
                return PartialView(order);
            }
            catch
            {
                TempData["Error"] = "Lỗi khi xử lý đơn hàng!";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Shipping(int id, int shipperID = 0)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    if (shipperID <= 0)
                        return Json(new { success = false, message = "Vui lòng chọn người giao hàng" });

                    await SalesDataService.ShipOrderAsync(id, shipperID);
                    TempData["Message"] = "Đã chuyển đơn hàng cho người giao hàng!";
                    return RedirectToAction("Detail", new { id = id });
                }

                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null) return RedirectToAction("Index");
                return PartialView(order);
            }
            catch
            {
                TempData["Error"] = "Lỗi khi chuyển giao hàng!";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Finish(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await SalesDataService.CompleteOrderAsync(id);
                    TempData["Message"] = "Đơn hàng đã được xác nhận hoàn tất!";
                    return RedirectToAction("Detail", new { id = id });
                }

                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null) return RedirectToAction("Index");
                return PartialView(order);
            }
            catch
            {
                TempData["Error"] = "Lỗi khi hoàn tất đơn hàng!";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    await SalesDataService.CancelOrderAsync(id);
                    TempData["Message"] = "Đã hủy đơn hàng thành công!";
                    return RedirectToAction("Detail", new { id = id });
                }

                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null) return RedirectToAction("Index");
                return PartialView(order);
            }
            catch
            {
                TempData["Error"] = "Lỗi khi hủy đơn hàng!";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                if (Request.Method == "POST")
                {
                    var userData = User.GetUserData();
                    if (userData == null || string.IsNullOrEmpty(userData.UserId))
                    {
                        TempData["Error"] = "Phiên đăng nhập không hợp lệ!";
                        return RedirectToAction("Detail", new { id = id });
                    }

                    int employeeID = Convert.ToInt32(userData.UserId);
                    await SalesDataService.RejectOrderAsync(id, employeeID);
                    TempData["Message"] = "Đã từ chối đơn hàng!";
                    return RedirectToAction("Detail", new { id = id });
                }

                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null) return RedirectToAction("Index");
                return PartialView(order);
            }
            catch
            {
                TempData["Error"] = "Lỗi khi từ chối đơn hàng!";
                return RedirectToAction("Index");
            }
        }

        #endregion

        [Authorize]
        public async Task<IActionResult> Detail(int id)
        {
            try
            {
                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null)
                {
                    return RedirectToAction("Index");
                }
                ViewBag.CanEdit = (order.Status == OrderStatusEnum.New);

                return View(order);
            }
            catch
            {
                TempData["Error"] = "Không thể tải chi tiết đơn hàng!";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> DetailProduct(int id)
        {
            try
            {
                var details = await SalesDataService.ListDetailsAsync(id);
                ViewBag.OrderID = id;
                return PartialView(details);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Lỗi tải chi tiết sản phẩm: {ex.Message}</div>");
            }
        }

        public async Task<IActionResult> EditDetail(int id = 0, int productId = 0)
        {
            try
            {
                var details = await SalesDataService.ListDetailsAsync(id);
                var item = details.FirstOrDefault(m => m.ProductID == productId);
                if (item == null)
                {
                    return Content("<div class='alert alert-danger'>Mặt hàng này không còn tồn tại trong hệ thống.</div>");
                }

                ViewBag.OrderID = id;
                return PartialView(item);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Lỗi: {ex.Message}</div>");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDetail(int orderId, int productId, int quantity, decimal salePrice)
        {
            try
            {
                if (quantity <= 0)
                    return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));

                if (salePrice < 0)
                    return Json(new ApiResult(0, "Giá bán không hợp lệ!"));

                var detailData = new OrderDetail()
                {
                    OrderID = orderId,
                    ProductID = productId,
                    Quantity = quantity,
                    SalePrice = salePrice
                };

                bool result = await SalesDataService.UpdateDetailAsync(detailData);

                if (result)
                {
                    return Json(new ApiResult(1));
                }
                else
                {
                    return Json(new ApiResult(0, "Không thể cập nhật dữ liệu vào cơ sở dữ liệu!"));
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, "Lỗi hệ thống: " + ex.Message));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteDetail(int id = 0, int productId = 0)
        {
            try
            {
                var model = await SalesDataService.GetDetailAsync(id, productId);
                if (model == null)
                {
                    return Content("<div class='alert alert-danger'>Mặt hàng này không còn tồn tại!</div>");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                return Content($"<div class='alert alert-danger'>Lỗi: {ex.Message}</div>");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDetailConfirm(int orderId, int productId)
        {
            try
            {
                bool result = await SalesDataService.DeleteDetailAsync(orderId, productId);
                if (result)
                    return Json(new ApiResult(1));

                return Json(new ApiResult(0, "Không thể xoá mặt hàng này!"));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, "Lỗi hệ thống: " + ex.Message));
            }
        }
    }
}