using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Partner;

namespace SV22T1020459.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhà cung cấp (Supplier) trên CSDL SQL Server
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL SQL Server</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một nhà cung cấp mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp cần bổ sung</param>
        /// <returns>Mã của nhà cung cấp vừa được bổ sung (SupplierID)</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Suppliers (SupplierName, ContactName, Province, Address, Phone, Email)
                VALUES (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                SELECT SCOPE_IDENTITY();";

            var result = await connection.ExecuteScalarAsync<int>(sql, data);
            return result;
        }

        /// <summary>
        /// Xóa nhà cung cấp có mã cho trước
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại là False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"DELETE FROM Suppliers WHERE SupplierID = @SupplierID";

            var result = await connection.ExecuteAsync(sql, new { SupplierID = id });
            return result > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhà cung cấp theo mã
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>Dữ liệu nhà cung cấp hoặc null nếu không tìm thấy</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";

            var result = await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierID = id });
            return result;
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp đã có dữ liệu liên quan (sản phẩm) hay chưa
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>True nếu đã được sử dụng (có sản phẩm tham chiếu), ngược lại là False</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Products WHERE SupplierID = @SupplierID) THEN 1 
                    ELSE 0 
                END";

            var result = await connection.ExecuteScalarAsync<bool>(sql, new { SupplierID = id });
            return result;
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách nhà cung cấp
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>Kết quả trả về dưới dạng PagedResult</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            // Xử lý chuỗi tìm kiếm (tìm tương đối LIKE)
            string searchValue = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : $"%{input.SearchValue}%";

            // Truy vấn đếm tổng số dòng dữ liệu thỏa mãn điều kiện
            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Suppliers
                WHERE (@SearchValue = N'') 
                   OR (SupplierName LIKE @SearchValue) 
                   OR (ContactName LIKE @SearchValue)";

            // Truy vấn lấy dữ liệu có phân trang
            string sqlQuery = "";
            if (input.PageSize > 0)
            {
                sqlQuery = @"
                    SELECT * FROM Suppliers
                    WHERE (@SearchValue = N'') 
                       OR (SupplierName LIKE @SearchValue) 
                       OR (ContactName LIKE @SearchValue)
                    ORDER BY SupplierName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            else // Trường hợp PageSize = 0 (không phân trang)
            {
                sqlQuery = @"
                    SELECT * FROM Suppliers
                    WHERE (@SearchValue = N'') 
                       OR (SupplierName LIKE @SearchValue) 
                       OR (ContactName LIKE @SearchValue)
                    ORDER BY SupplierName";
            }

            // Thực thi truy vấn
            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new { SearchValue = searchValue });
            var dataItems = await connection.QueryAsync<Supplier>(sqlQuery, new
            {
                SearchValue = searchValue,
                Offset = input.Offset,
                PageSize = input.PageSize
            });

            // Trả về kết quả
            return new PagedResult<Supplier>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin của một nhà cung cấp
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp với các thông tin mới</param>
        /// <returns>True nếu cập nhật thành công, ngược lại là False</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Suppliers 
                SET SupplierName = @SupplierName, 
                    ContactName = @ContactName, 
                    Province = @Province, 
                    Address = @Address, 
                    Phone = @Phone, 
                    Email = @Email
                WHERE SupplierID = @SupplierID";

            var result = await connection.ExecuteAsync(sql, data);
            return result > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int supplierID = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Suppliers WHERE Email = @Email AND SupplierID <> @SupplierID) THEN 1 
                    ELSE 0 
                END";
            var result = await connection.ExecuteScalarAsync<bool>(sql, new { Email = email, SupplierID = supplierID });
            return !result;
        }
    }
}