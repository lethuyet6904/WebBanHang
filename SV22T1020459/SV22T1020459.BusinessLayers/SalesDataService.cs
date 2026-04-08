using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.DataLayers.SQLServer;
using SV22T1020459.Models;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Sales;

namespace SV22T1020459.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        /// <summary>
        /// Constructor khởi tạo kết nối Database
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Order

        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            try { return await orderDB.ListAsync(input); }
            catch (Exception ex) { throw new Exception("Lỗi khi tải danh sách Đơn hàng: " + ex.Message); }
        }

        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            try { return await orderDB.GetAsync(orderID); }
            catch { return null; }
        }

        public static async Task<int> AddOrderAsync(int customerID = 0, string province = "", string address = "")
        {
            try
            {
                var order = new Order
                {
                    // Nếu customerID = 0 (khách vãng lai), set null để không vi phạm khóa ngoại
                    CustomerID = customerID == 0 ? null : customerID,
                    DeliveryProvince = province,
                    DeliveryAddress = address,
                    Status = OrderStatusEnum.New,
                    OrderTime = DateTime.Now
                };
                return await orderDB.AddAsync(order);
            }
            catch { return 0; }
        }

        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            try { return await orderDB.UpdateAsync(data); }
            catch { return false; }
        }

        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            try { return await orderDB.DeleteAsync(orderID); }
            catch { return false; }
        }

        #endregion

        #region Order Status Processing

        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            try
            {
                var order = await orderDB.GetAsync(orderID);
                if (order == null || order.Status != OrderStatusEnum.New)
                    return false;

                order.EmployeeID = employeeID;
                order.AcceptTime = DateTime.Now;
                order.Status = OrderStatusEnum.Accepted;

                return await orderDB.UpdateAsync(order);
            }
            catch { return false; }
        }

        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            try
            {
                var order = await orderDB.GetAsync(orderID);
                if (order == null || order.Status != OrderStatusEnum.New)
                    return false;

                order.EmployeeID = employeeID;
                order.FinishedTime = DateTime.Now;
                order.Status = OrderStatusEnum.Rejected;

                return await orderDB.UpdateAsync(order);
            }
            catch { return false; }
        }

        public static async Task<bool> CancelOrderAsync(int orderID)
        {
            try
            {
                var order = await orderDB.GetAsync(orderID);
                if (order == null)
                    return false;

                if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
                    return false;

                order.FinishedTime = DateTime.Now;
                order.Status = OrderStatusEnum.Cancelled;

                return await orderDB.UpdateAsync(order);
            }
            catch { return false; }
        }

        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            try
            {
                var order = await orderDB.GetAsync(orderID);
                if (order == null || order.Status != OrderStatusEnum.Accepted)
                    return false;

                order.ShipperID = shipperID;
                order.ShippedTime = DateTime.Now;
                order.Status = OrderStatusEnum.Shipping;

                return await orderDB.UpdateAsync(order);
            }
            catch { return false; }
        }

        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            try
            {
                var order = await orderDB.GetAsync(orderID);
                if (order == null || order.Status != OrderStatusEnum.Shipping)
                    return false;

                order.FinishedTime = DateTime.Now;
                order.Status = OrderStatusEnum.Completed;

                return await orderDB.UpdateAsync(order);
            }
            catch { return false; }
        }

        #endregion

        #region Order Detail

        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            try { return await orderDB.ListDetailsAsync(orderID); }
            catch { return new List<OrderDetailViewInfo>(); }
        }

        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            try { return await orderDB.GetDetailAsync(orderID, productID); }
            catch { return null; }
        }

        public static async Task<bool> AddDetailAsync(OrderDetail data)
        {
            try { return await orderDB.AddDetailAsync(data); }
            catch { return false; }
        }

        public static async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            try { return await orderDB.UpdateDetailAsync(data); }
            catch { return false; }
        }

        public static async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            try { return await orderDB.DeleteDetailAsync(orderID, productID); }
            catch { return false; }
        }

        #endregion

        #region Dashboard

        /// <summary>Lấy tổng doanh thu hôm nay từ các đơn hàng đã hoàn thành</summary>
        public static async Task<decimal> GetTodayRevenueAsync()
        {
            try { return await orderDB.GetTodayRevenueAsync(); }
            catch { return 0; }
        }

        /// <summary>Lấy doanh thu theo từng tháng trong năm chỉ định (12 phần tử)</summary>
        public static async Task<List<decimal>> GetMonthlyRevenueAsync(int year)
        {
            try { return await orderDB.GetMonthlyRevenueAsync(year); }
            catch { return Enumerable.Repeat(0m, 12).ToList(); }
        }

        /// <summary>Lấy top N sản phẩm bán chạy nhất</summary>
        public static async Task<List<TopProductItem>> GetTopProductsAsync(int top = 5)
        {
            try { return await orderDB.GetTopProductsAsync(top); }
            catch { return new List<TopProductItem>(); }
        }

        #endregion
    }
}