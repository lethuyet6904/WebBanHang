namespace SV22T1020459.Models.Sales
{
    /// <summary>
    /// lấy thông tin chi tiết của một đơn hàng, bao gồm thông tin chung và danh sách chi tiết
    /// </summary>
    public class OrderDetailsModel
    {
  
        public OrderViewInfo? Order { get; set; }
        /// <summary>
        /// Gets or sets the collection of order detail view information associated with the order.
        /// </summary>
        public List<OrderDetailViewInfo> Details { get; set; } = new List<OrderDetailViewInfo>();
    }
}