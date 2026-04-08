using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020459.BusinessLayers;
using SV22T1020459.Models;
using SV22T1020459.Models.Catalog;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Sales;

namespace SV22T1020459.Admin.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        /// <summary>
        /// Hiển thị trang chủ của ứng dụng với dữ liệu thống kê
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var model = new DashboardModel();
            try
            {
                var customerData = await PartnerDataService.ListCustomersAsync(new PaginationSearchInput { Page = 1, PageSize = 1, SearchValue = "" });
                model.TotalCustomers = customerData.RowCount;

                var productData = await CatalogDataService.ListProductsAsync(new ProductSearchInput { Page = 1, PageSize = 1, SearchValue = "" });
                model.TotalProducts = productData.RowCount;

                var orderData = await SalesDataService.ListOrdersAsync(new OrderSearchInput { Page = 1, PageSize = 1, SearchValue = "", Status = 0 });
                model.TotalOrders = orderData.RowCount;

                var pendingOrderData = await SalesDataService.ListOrdersAsync(new OrderSearchInput { Page = 1, PageSize = 5, SearchValue = "", Status = (OrderStatusEnum)1 });
                model.TotalPendingOrders = pendingOrderData.RowCount;
                model.PendingOrders = pendingOrderData.DataItems;

                model.TodayRevenue = await SalesDataService.GetTodayRevenueAsync();

                model.MonthlyRevenue = await SalesDataService.GetMonthlyRevenueAsync(DateTime.Now.Year);

                model.TopProducts = await SalesDataService.GetTopProductsAsync(5);

                return View(model);
            }
            catch (Exception ex)
            {
                return View(model);
            }
        }
    }
}