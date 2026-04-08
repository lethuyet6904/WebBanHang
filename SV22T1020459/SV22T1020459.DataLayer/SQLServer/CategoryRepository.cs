using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.Models.Catalog;
using SV22T1020459.Models.Common;

namespace SV22T1020459.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho loại hàng (Category) trên CSDL SQL Server
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL SQL Server</param>
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một loại hàng mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng cần bổ sung</param>
        /// <returns>Mã của loại hàng vừa được bổ sung (CategoryID)</returns>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Categories (CategoryName, Description)
                VALUES (@CategoryName, @Description);
                SELECT SCOPE_IDENTITY();";

            var result = await connection.ExecuteScalarAsync<int>(sql, data);
            return result;
        }

        /// <summary>
        /// Xóa loại hàng có mã cho trước
        /// </summary>
        /// <param name="id">Mã loại hàng cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại là False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"DELETE FROM Categories WHERE CategoryID = @CategoryID";

            var result = await connection.ExecuteAsync(sql, new { CategoryID = id });
            return result > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một loại hàng theo mã
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>Dữ liệu loại hàng hoặc null nếu không tìm thấy</returns>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"SELECT * FROM Categories WHERE CategoryID = @CategoryID";

            var result = await connection.QueryFirstOrDefaultAsync<Category>(sql, new { CategoryID = id });
            return result;
        }

        /// <summary>
        /// Kiểm tra xem loại hàng đã được sử dụng hay chưa
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>True nếu đã được sử dụng (có sản phẩm thuộc loại này), ngược lại là False</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Products WHERE CategoryID = @CategoryID) THEN 1 
                    ELSE 0 
                END";

            var result = await connection.ExecuteScalarAsync<bool>(sql, new { CategoryID = id });
            return result;
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh mục loại hàng
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>Kết quả trả về dưới dạng PagedResult</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            // Xử lý chuỗi tìm kiếm
            string searchValue = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : $"%{input.SearchValue}%";

            // Đếm tổng số lượng bản ghi thỏa mãn điều kiện
            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Categories
                WHERE (@SearchValue = N'') 
                   OR (CategoryName LIKE @SearchValue) 
                   OR (Description LIKE @SearchValue)";

            string sqlQuery;

            if (input.PageSize > 0)
            {
                sqlQuery = @"
                    SELECT * FROM Categories
                    WHERE (@SearchValue = N'') 
                       OR (CategoryName LIKE @SearchValue) 
                       OR (Description LIKE @SearchValue)
                    ORDER BY CategoryName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            else
            {
                sqlQuery = @"
                    SELECT * FROM Categories
                    WHERE (@SearchValue = N'') 
                       OR (CategoryName LIKE @SearchValue) 
                       OR (Description LIKE @SearchValue)
                    ORDER BY CategoryName";
            }

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new { SearchValue = searchValue });

            var dataItems = await connection.QueryAsync<Category>(sqlQuery, new
            {
                SearchValue = searchValue,
                Offset = input.Offset,
                PageSize = input.PageSize
            });

            return new PagedResult<Category>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin của một loại hàng
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng với các thông tin mới</param>
        /// <returns>True nếu cập nhật thành công, ngược lại là False</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Categories 
                SET CategoryName = @CategoryName, 
                    Description = @Description
                WHERE CategoryID = @CategoryID";

            var result = await connection.ExecuteAsync(sql, data);
            return result > 0;
        }
    }
}