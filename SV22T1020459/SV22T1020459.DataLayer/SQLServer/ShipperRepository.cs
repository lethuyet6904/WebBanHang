using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Partner;

namespace SV22T1020459.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho người giao hàng (Shipper) trên CSDL SQL Server
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL SQL Server</param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một người giao hàng mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng cần bổ sung</param>
        /// <returns>Mã của người giao hàng vừa được bổ sung (ShipperID)</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Shippers (ShipperName, Phone)
                VALUES (@ShipperName, @Phone);
                SELECT SCOPE_IDENTITY();";

            // Thực thi câu lệnh và lấy về ID tự tăng vừa được tạo
            var result = await connection.ExecuteScalarAsync<int>(sql, data);
            return result;
        }

        /// <summary>
        /// Xóa người giao hàng có mã cho trước
        /// </summary>
        /// <param name="id">Mã người giao hàng cần xóa</param>
        /// <returns>True nếu xóa thành công, ngược lại là False</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"DELETE FROM Shippers WHERE ShipperID = @ShipperID";

            var result = await connection.ExecuteAsync(sql, new { ShipperID = id });
            return result > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một người giao hàng theo mã
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>Dữ liệu người giao hàng hoặc null nếu không tìm thấy</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"SELECT * FROM Shippers WHERE ShipperID = @ShipperID";

            var result = await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { ShipperID = id });
            return result;
        }

        /// <summary>
        /// Kiểm tra xem người giao hàng đã có dữ liệu liên quan hay chưa (ví dụ: đã từng giao đơn hàng nào chưa)
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>True nếu đã được sử dụng (có trong bảng Orders), ngược lại là False</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Orders WHERE ShipperID = @ShipperID) THEN 1 
                    ELSE 0 
                END";

            var result = await connection.ExecuteScalarAsync<bool>(sql, new { ShipperID = id });
            return result;
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách người giao hàng
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>Kết quả trả về dưới dạng PagedResult</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            // Xử lý chuỗi tìm kiếm (nếu rỗng thì lấy tất cả, nếu có thì tìm tương đối)
            string searchValue = string.IsNullOrWhiteSpace(input.SearchValue)
                ? ""
                : $"%{input.SearchValue}%";

            // Đếm tổng số lượng bản ghi thỏa mãn điều kiện tìm kiếm
            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Shippers
                WHERE (@SearchValue = N'') 
                   OR (ShipperName LIKE @SearchValue) 
                   OR (Phone LIKE @SearchValue)";

            string sqlQuery;

            if (input.PageSize > 0)
            {
                // Truy vấn lấy dữ liệu có phân trang
                sqlQuery = @"
                    SELECT * FROM Shippers
                    WHERE (@SearchValue = N'') 
                       OR (ShipperName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                    ORDER BY ShipperName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }
            else
            {
                // Truy vấn lấy tất cả dữ liệu (không phân trang)
                sqlQuery = @"
                    SELECT * FROM Shippers
                    WHERE (@SearchValue = N'') 
                       OR (ShipperName LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue)
                    ORDER BY ShipperName";
            }

            // Thực hiện truy vấn bất đồng bộ (chạy song song cũng được nhưng Dapper thường gọi tuần tự trên cùng connection)
            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, new { SearchValue = searchValue });

            var dataItems = await connection.QueryAsync<Shipper>(sqlQuery, new
            {
                SearchValue = searchValue,
                Offset = input.Offset,
                PageSize = input.PageSize
            });

            // Trả về đối tượng PagedResult đã được thiết kế sẵn
            return new PagedResult<Shipper>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin của một người giao hàng
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng với các thông tin mới</param>
        /// <returns>True nếu cập nhật thành công, ngược lại là False</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Shippers 
                SET ShipperName = @ShipperName, 
                    Phone = @Phone
                WHERE ShipperID = @ShipperID";

            var result = await connection.ExecuteAsync(sql, data);
            return result > 0;
        }
    }
}