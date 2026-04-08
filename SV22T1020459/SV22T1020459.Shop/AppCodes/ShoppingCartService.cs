using SV22T1020459.Models.Sales;

namespace SV22T1020459.Shop
{
    /// <summary>
    /// Cung cấp các chức năng xử lý trên giỏ hàng
    /// (Giỏ hàng lưu trong session phân tách theo từng User)
    /// </summary>
    public static class ShoppingCartService
    {
        /// <summary>
        /// Hàm sinh ra tên Session riêng biệt cho từng người dùng
        /// </summary>
        private static string GetCartKey()
        {
            // Lấy thông tin User hiện tại từ HttpContext
            var user = ApplicationContext.HttpContext?.User.GetUserData();

            if (user != null && !string.IsNullOrEmpty(user.UserId))
            {
                // Nếu đã đăng nhập, giỏ hàng sẽ mang tên chứa mã User (VD: ShoppingCart_1)
                return $"ShoppingCart_{user.UserId}";
            }

            // Nếu là khách vãng lai chưa đăng nhập
            return "ShoppingCart_Guest";
        }

        /// <summary>
        /// Lấy giỏ hàng từ session
        /// </summary>
        public static List<OrderDetailViewInfo> GetShoppingCart()
        {
            string cartKey = GetCartKey();
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(cartKey);
            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(cartKey, cart);
            }
            return cart;
        }

        /// <summary>
        /// Lấy thông tin 1 mặt hàng từ giỏ hàng
        /// </summary>
        public static OrderDetailViewInfo? GetCartItem(int productID)
        {
            var cart = GetShoppingCart();
            return cart.Find(m => m.ProductID == productID);
        }

        /// <summary>
        /// Thêm hàng vào giỏ hàng
        /// </summary>
        public static void AddCartItem(OrderDetailViewInfo item)
        {
            var cart = GetShoppingCart();
            var existsItem = cart.Find(m => m.ProductID == item.ProductID);
            if (existsItem == null)
            {
                cart.Add(item);
            }
            else
            {
                existsItem.Quantity += item.Quantity;
                existsItem.SalePrice = item.SalePrice;
            }
            ApplicationContext.SetSessionData(GetCartKey(), cart);
        }

        /// <summary>
        /// Cập nhật số lượng và giá của một mặt hàng trong giỏ hàng
        /// </summary>
        public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            if (item != null)
            {
                item.Quantity = quantity;
                item.SalePrice = salePrice;
                ApplicationContext.SetSessionData(GetCartKey(), cart);
            }
        }

        /// <summary>
        /// Xóa một mặt hàng ra khỏi giỏ hàng
        /// </summary>
        public static void RemoveCartItem(int productID)
        {
            var cart = GetShoppingCart();
            int index = cart.FindIndex(m => m.ProductID == productID);
            if (index >= 0)
            {
                cart.RemoveAt(index);
                ApplicationContext.SetSessionData(GetCartKey(), cart);
            }
        }

        /// <summary>
        /// Xóa giỏ hàng
        /// </summary>
        public static void ClearCart()
        {
            var cart = new List<OrderDetailViewInfo>();
            ApplicationContext.SetSessionData(GetCartKey(), cart);
        }
    }
}