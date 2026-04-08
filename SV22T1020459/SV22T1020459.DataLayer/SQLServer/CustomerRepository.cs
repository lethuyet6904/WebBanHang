using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Partner;

namespace SV22T1020459.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho khách hàng (Customer) trên CSDL SQL Server
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL SQL Server</param>
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một khách hàng mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng cần bổ sung</param>
        /// <returns>Mã của khách hàng vừa được bổ sung (CustomerID)</returns>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Customers (CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                VALUES (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, @IsLocked);
                SELECT SCOPE_IDENTITY();";

            var result = await connection.ExecuteScalarAsync<int>(sql, data);
            return result;
        }

        /// <summary>
        /// Xóa khách hàng có mã cho trước
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại là False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"DELETE FROM Customers WHERE CustomerID = @CustomerID";

            var result = await connection.ExecuteAsync(sql, new { CustomerID = id });
            return result > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một khách hàng theo mã
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>Dữ liệu khách hàng hoặc null nếu không tìm thấy</returns>
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"SELECT * FROM Customers WHERE CustomerID = @CustomerID";

            var result = await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerID = id });
            return result;
        }

        /// <summary>
        /// Kiểm tra xem khách hàng đã phát sinh giao dịch (đơn hàng) hay chưa
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>True nếu đã được sử dụng (có trong bảng Orders), ngược lại là False</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Orders WHERE CustomerID = @CustomerID) THEN 1 
                    ELSE 0 
                END";

            var result = await connection.ExecuteScalarAsync<bool>(sql, new { CustomerID = id });
            return result;
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách khách hàng
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>Kết quả trả về dưới dạng PagedResult</returns>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            string searchValue = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : $"%{input.SearchValue}%";

            // Đếm tổng số lượng bản ghi thỏa mãn điều kiện
            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Customers
                WHERE (@SearchValue = N'') 
                   OR (CustomerName LIKE @SearchValue) 
                   OR (ContactName LIKE @SearchValue)
                   OR (Phone LIKE @SearchValue)
                   OR (Email LIKE @SearchValue)";

            string sqlQuery;

            if (input.PageSize > 0)
            {
                sqlQuery = @"
                    SELECT * FROM Customers
                    WHERE (@SearchValue = N'') 
                       OR (CustomerName LIKE @SearchValue) 
                       OR (ContactName LIKE @SearchValue)
                       OR (Phone LIKE @SearchValue)
                       OR (Email LIKE @SearchValue)
                    ORDER BY CustomerName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            else
            {
                sqlQuery = @"
                    SELECT * FROM Customers
                    WHERE (@SearchValue = N'') 
                       OR (CustomerName LIKE @SearchValue) 
                       OR (ContactName LIKE @SearchValue)
                       OR (Phone LIKE @SearchValue)
                       OR (Email LIKE @SearchValue)
                    ORDER BY CustomerName";
            }

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new { SearchValue = searchValue });

            var dataItems = await connection.QueryAsync<Customer>(sqlQuery, new
            {
                SearchValue = searchValue,
                Offset = input.Offset,
                PageSize = input.PageSize
            });

            return new PagedResult<Customer>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin của một khách hàng
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng với các thông tin mới</param>
        /// <returns>True nếu cập nhật thành công, ngược lại là False</returns>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Customers 
                SET CustomerName = @CustomerName, 
                    ContactName = @ContactName, 
                    Province = @Province, 
                    Address = @Address, 
                    Phone = @Phone, 
                    Email = @Email,
                    IsLocked = @IsLocked
                WHERE CustomerID = @CustomerID";

            var result = await connection.ExecuteAsync(sql, data);
            return result > 0;
        }

        /// <summary>
        /// Kiểm tra xem một địa chỉ email có hợp lệ (chưa bị trùng) hay không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">Mã khách hàng (0 nếu là thêm mới, khác 0 nếu là cập nhật)</param>
        /// <returns>True nếu email hợp lệ (không trùng với người khác), False nếu đã tồn tại</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*) 
                FROM Customers 
                WHERE Email = @Email AND CustomerID <> @Id";

            // Nếu count == 0 tức là email chưa có ai sử dụng (hợp lệ)
            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, Id = id });
            return count == 0;
        }
    }
}