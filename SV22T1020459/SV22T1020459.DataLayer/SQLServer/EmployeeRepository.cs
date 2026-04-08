using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.HR;

namespace SV22T1020459.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhân viên (Employee) trên CSDL SQL Server
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL SQL Server</param>
        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một nhân viên mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu nhân viên cần bổ sung</param>
        /// <returns>Mã của nhân viên vừa được bổ sung (EmployeeID)</returns>
        public async Task<int> AddAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Employees (FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
                VALUES (@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);
                SELECT SCOPE_IDENTITY();";

            var result = await connection.ExecuteScalarAsync<int>(sql, data);
            return result;
        }

        /// <summary>
        /// Xóa nhân viên có mã cho trước
        /// </summary>
        /// <param name="id">Mã nhân viên cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại là False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"DELETE FROM Employees WHERE EmployeeID = @EmployeeID";

            var result = await connection.ExecuteAsync(sql, new { EmployeeID = id });
            return result > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên theo mã
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>Dữ liệu nhân viên hoặc null nếu không tìm thấy</returns>
        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";

            var result = await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
            return result;
        }

        /// <summary>
        /// Kiểm tra xem nhân viên đã phát sinh nghiệp vụ (đơn hàng) hay chưa
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>True nếu đã được sử dụng (có trong bảng Orders), ngược lại là False</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Orders WHERE EmployeeID = @EmployeeID) THEN 1 
                    ELSE 0 
                END";

            var result = await connection.ExecuteScalarAsync<bool>(sql, new { EmployeeID = id });
            return result;
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách nhân viên
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>Kết quả trả về dưới dạng PagedResult</returns>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            string searchValue = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : $"%{input.SearchValue}%";

            // Đếm tổng số lượng bản ghi thỏa mãn điều kiện
            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Employees
                WHERE (@SearchValue = N'') 
                   OR (FullName LIKE @SearchValue) 
                   OR (Email LIKE @SearchValue)
                   OR (Phone LIKE @SearchValue)";

            string sqlQuery;

            if (input.PageSize > 0)
            {
                sqlQuery = @"
                    SELECT * FROM Employees
                    WHERE (@SearchValue = N'') 
                       OR (FullName LIKE @SearchValue) 
                       OR (Email LIKE @SearchValue)
                       OR (Phone LIKE @SearchValue)
                    ORDER BY FullName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            else
            {
                sqlQuery = @"
                    SELECT * FROM Employees
                    WHERE (@SearchValue = N'') 
                       OR (FullName LIKE @SearchValue) 
                       OR (Email LIKE @SearchValue)
                       OR (Phone LIKE @SearchValue)
                    ORDER BY FullName";
            }

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new { SearchValue = searchValue });

            var dataItems = await connection.QueryAsync<Employee>(sqlQuery, new
            {
                SearchValue = searchValue,
                Offset = input.Offset,
                PageSize = input.PageSize
            });

            return new PagedResult<Employee>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin của một nhân viên
        /// </summary>
        /// <param name="data">Dữ liệu nhân viên với các thông tin mới</param>
        /// <returns>True nếu cập nhật thành công, ngược lại là False</returns>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Employees 
                SET FullName = @FullName, 
                    BirthDate = @BirthDate, 
                    Address = @Address, 
                    Phone = @Phone, 
                    Email = @Email,
                    Photo = @Photo,
                    IsWorking = @IsWorking
                WHERE EmployeeID = @EmployeeID";

            var result = await connection.ExecuteAsync(sql, data);
            return result > 0;
        }

        /// <summary>
        /// Kiểm tra xem một địa chỉ email có hợp lệ (chưa bị trùng) hay không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">Mã nhân viên (0 nếu là thêm mới, khác 0 nếu là cập nhật)</param>
        /// <returns>True nếu email hợp lệ (không trùng với nhân viên khác), False nếu đã tồn tại</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*) 
                FROM Employees 
                WHERE Email = @Email AND EmployeeID <> @Id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, Id = id });
            return count == 0;
        }

        public async Task<bool> UpdateRoleAsync(int id, string roleNames)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"UPDATE Employees SET RoleNames = @RoleNames WHERE EmployeeID = @EmployeeID";
            var result = await connection.ExecuteAsync(sql, new { RoleNames = roleNames, EmployeeID = id });
            return result > 0;
        }
    }
}