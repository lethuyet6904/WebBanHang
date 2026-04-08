using SV22T1020459.Models.Sales;

namespace SV22T1020459.Models
{
    public class DashboardModel
    {
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalPendingOrders { get; set; }
        public List<OrderViewInfo> PendingOrders { get; set; } = new List<OrderViewInfo>();

        /// <summary>Doanh thu hôm nay (đơn hoàn thành)</summary>
        public decimal TodayRevenue { get; set; }

        /// <summary>Doanh thu theo từng tháng trong năm hiện tại (index 0 = tháng 1)</summary>
        public List<decimal> MonthlyRevenue { get; set; } = new List<decimal>();

        /// <summary>Top sản phẩm bán chạy</summary>
        public List<TopProductItem> TopProducts { get; set; } = new List<TopProductItem>();
    }

    public class TopProductItem
    {
        public string ProductName { get; set; } = "";
        public int TotalQuantity { get; set; }
    }
}