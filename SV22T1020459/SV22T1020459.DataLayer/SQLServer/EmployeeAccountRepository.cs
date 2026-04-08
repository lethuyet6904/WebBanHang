using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.Models.HR;
using SV22T1020459.Models.Security;

namespace SV22T1020459.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tài khoản của nhân viên
    /// </summary>
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL SQL Server</param>
        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Xác thực tài khoản của nhân viên
        /// </summary>
        /// <param name="userName">Tên đăng nhập (Email)</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>Thông tin tài khoản nếu hợp lệ, ngược lại trả về null</returns>
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            // Ép kiểu EmployeeID thành chuỗi (CAST) để khớp với thuộc tính UserId của UserAccount
            // Thêm điều kiện IsWorking = 1 để đảm bảo tài khoản chưa bị khóa/nghỉ việc
            string sql = @"
                SELECT CAST(EmployeeID AS NVARCHAR(50)) AS UserId, 
                       Email AS UserName, 
                       FullName AS DisplayName, 
                       Email, 
                       Photo, 
                       RoleNames
                FROM Employees 
                WHERE Email = @UserName AND Password = @Password AND IsWorking = 1";

            var result = await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new
            {
                UserName = userName,
                Password = password
            });

            return result;
        }

        /// <summary>
        /// Đổi mật khẩu cho tài khoản nhân viên
        /// </summary>
        /// <param name="userName">Tên đăng nhập (Email)</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>True nếu đổi thành công, ngược lại là False</returns>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Employees 
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