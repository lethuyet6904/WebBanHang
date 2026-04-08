using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.Models.Security;

namespace SV22T1020459.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tài khoản của khách hàng
    /// </summary>
    public class CustomerAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL SQL Server</param>
        public CustomerAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Xác thực tài khoản của khách hàng
        /// </summary>
        /// <param name="userName">Tên đăng nhập (Email của khách hàng)</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>Thông tin tài khoản nếu hợp lệ, ngược lại trả về null</returns>
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            // Map CustomerID sang UserId, CustomerName sang DisplayName.
            // Vì khách hàng thường không có Photo và RoleNames riêng biệt trong hệ thống này, 
            // ta gán cứng một giá trị rỗng (hoặc một role mặc định nếu cần).
            string sql = @"
                SELECT CAST(CustomerID AS NVARCHAR(50)) AS UserId, 
                       Email AS UserName, 
                       CustomerName AS DisplayName, 
                       Email, 
                       N'' AS Photo, 
                       N'' AS RoleNames
                FROM Customers 
                WHERE Email = @UserName 
                  AND Password = @Password 
                  AND (IsLocked = 0 OR IsLocked IS NULL)";

            var result = await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new
            {
                UserName = userName,
                Password = password
            });

            return result;
        }

        /// <summary>
        /// Đổi mật khẩu cho tài khoản khách hàng
        /// </summary>
        /// <param name="userName">Tên đăng nhập (Email)</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>True nếu đổi thành công, ngược lại là False</returns>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Customers 
                SET Password = @Password 
                WHERE Email = @UserName";

            var result = await connection.ExecuteAsync(sql, new
            {
                UserName = userName,
                Password = password
            });

            return result > 0;
        }
    }
}