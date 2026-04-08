namespace SV22T1020459.BusinessLayers
{
    /// <summary>
    /// Lớp lưu giữ các thông tin cấu hình cần sử dụng cho BusinessLayer
    /// </summary>
    public class Configuration
    {
        private static string _connectionString = "";

        /// <summary>
        /// Khởi tạo cấu hình cho BusinessLayer
        /// (Hàm này phải được gọi trước khi chay ứng dụng)
        /// </summary>
        /// <param name="connectionString"></param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }


        /// <summary>
        /// Thuộc tính trả về chuổi tham số kết nối đến csdl của hệ thống
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
