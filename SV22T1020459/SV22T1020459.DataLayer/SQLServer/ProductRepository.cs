using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.Models.Catalog;
using SV22T1020459.Models.Common;

namespace SV22T1020459.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho mặt hàng (Product), kèm theo ảnh và thuộc tính của nó
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL SQL Server</param>
        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Xử lý thông tin chính của Mặt hàng (Product)

        /// <summary>
        /// Bổ sung mặt hàng mới
        /// </summary>
        /// <param name="data">Dữ liệu mặt hàng cần thêm</param>
        /// <returns>Mã mặt hàng vừa được bổ sung</returns>
        public async Task<int> AddAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Products (ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                VALUES (@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                SELECT SCOPE_IDENTITY();";

            var result = await connection.ExecuteScalarAsync<int>(sql, data);
            return result;
        }

        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng cần xóa</param>
        /// <returns>True nếu xóa thành công, False nếu thất bại</returns>
        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            // Xóa các dữ liệu liên quan trước (ảnh và thuộc tính) để tránh lỗi vi phạm khóa ngoại
            string sql = @"
                DELETE FROM ProductPhotos WHERE ProductID = @ProductID;
                DELETE FROM ProductAttributes WHERE ProductID = @ProductID;
                DELETE FROM Products WHERE ProductID = @ProductID;";

            var result = await connection.ExecuteAsync(sql, new { ProductID = productID });
            return result > 0;
        }

        /// <summary>
        /// Lấy thông tin 1 mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Đối tượng mặt hàng hoặc null nếu không tìm thấy</returns>
        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"SELECT * FROM Products WHERE ProductID = @ProductID";

            var result = await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductID = productID });
            return result;
        }

        /// <summary>
        /// Kiểm tra mặt hàng có dữ liệu liên quan (như đơn hàng) hay không
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>True nếu đang được sử dụng, False nếu không</returns>
        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM OrderDetails WHERE ProductID = @ProductID) THEN 1 
                    ELSE 0 
                END";

            var result = await connection.ExecuteScalarAsync<bool>(sql, new { ProductID = productID });
            return result;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách mặt hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang chứa danh sách mặt hàng</returns>
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            string searchValue = string.IsNullOrWhiteSpace(input.SearchValue) ? "" : $"%{input.SearchValue}%";

            // Đếm tổng số dòng dữ liệu thỏa mãn điều kiện
            string sqlCount = @"
                SELECT COUNT(*) 
                FROM Products 
                WHERE (@SearchValue = N'' OR ProductName LIKE @SearchValue)
                  AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                  AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                  AND (Price >= @MinPrice)
                  AND (@MaxPrice <= 0 OR Price <= @MaxPrice)";

            // Lấy danh sách phân trang
            string sqlQuery = @"
                SELECT * FROM Products 
                WHERE (@SearchValue = N'' OR ProductName LIKE @SearchValue)
                  AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                  AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                  AND (Price >= @MinPrice)
                  AND (@MaxPrice <= 0 OR Price <= @MaxPrice)
                ORDER BY ProductName";

            if (input.PageSize > 0)
            {
                sqlQuery += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            }

            var parameters = new
            {
                SearchValue = searchValue,
                CategoryID = input.CategoryID,
                SupplierID = input.SupplierID,
                MinPrice = input.MinPrice,
                MaxPrice = input.MaxPrice,
                Offset = input.Offset,
                PageSize = input.PageSize
            };

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);
            var dataItems = await connection.QueryAsync<Product>(sqlQuery, parameters);

            return new PagedResult<Product>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = dataItems.ToList()
            };
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        /// <param name="data">Dữ liệu mặt hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công, False nếu thất bại</returns>
        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Products 
                SET ProductName = @ProductName,
                    ProductDescription = @ProductDescription,
                    SupplierID = @SupplierID,
                    CategoryID = @CategoryID,
                    Unit = @Unit,
                    Price = @Price,
                    Photo = @Photo,
                    IsSelling = @IsSelling
                WHERE ProductID = @ProductID";

            var result = await connection.ExecuteAsync(sql, data);
            return result > 0;
        }

        #endregion

        #region Xử lý Thuộc tính (Attributes) của mặt hàng

        /// <summary>
        /// Bổ sung thuộc tính mới cho mặt hàng
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính</param>
        /// <returns>Mã thuộc tính vừa thêm</returns>
        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO ProductAttributes (ProductID, AttributeName, AttributeValue, DisplayOrder)
                VALUES (@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                SELECT SCOPE_IDENTITY();";

            var result = await connection.ExecuteScalarAsync<long>(sql, data);
            return result;
        }

        /// <summary>
        /// Xóa thuộc tính
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính cần xóa</param>
        /// <returns>True nếu xóa thành công, False nếu thất bại</returns>
        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";

            var result = await connection.ExecuteAsync(sql, new { AttributeID = attributeID });
            return result > 0;
        }

        /// <summary>
        /// Lấy thông tin của một thuộc tính
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính</param>
        /// <returns>Đối tượng thuộc tính hoặc null</returns>
        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"SELECT * FROM ProductAttributes WHERE AttributeID = @AttributeID";

            var result = await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
            return result;
        }

        /// <summary>
        /// Lấy danh sách thuộc tính của mặt hàng
        /// </summary>
        /// <param name="productID">Mã của mặt hàng</param>
        /// <returns>Danh sách các thuộc tính</returns>
        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT * FROM ProductAttributes 
                WHERE ProductID = @ProductID 
                ORDER BY DisplayOrder";

            var result = await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID });
            return result.ToList();
        }

        /// <summary>
        /// Cập nhật thuộc tính của mặt hàng
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính cần cập nhật</param>
        /// <returns>True nếu thành công, False nếu thất bại</returns>
        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE ProductAttributes 
                SET AttributeName = @AttributeName,
                    AttributeValue = @AttributeValue,
                    DisplayOrder = @DisplayOrder
                WHERE AttributeID = @AttributeID";

            var result = await connection.ExecuteAsync(sql, data);
            return result > 0;
        }

        #endregion

        #region Xử lý Ảnh (Photos) của mặt hàng

        /// <summary>
        /// Bổ sung ảnh mới cho mặt hàng
        /// </summary>
        /// <param name="data">Dữ liệu ảnh</param>
        /// <returns>Mã ảnh vừa thêm</returns>
        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO ProductPhotos (ProductID, Photo, Description, DisplayOrder, IsHidden)
                VALUES (@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                SELECT SCOPE_IDENTITY();";

            var result = await connection.ExecuteScalarAsync<long>(sql, data);
            return result;
        }

        /// <summary>
        /// Xóa ảnh
        /// </summary>
        /// <param name="photoID">Mã ảnh cần xóa</param>
        /// <returns>True nếu xóa thành công, False nếu thất bại</returns>
        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";

            var result = await connection.ExecuteAsync(sql, new { PhotoID = photoID });
            return result > 0;
        }

        /// <summary>
        /// Lấy thông tin 1 ảnh của mặt hàng
        /// </summary>
        /// <param name="photoID">Mã ảnh</param>
        /// <returns>Đối tượng ảnh hoặc null</returns>
        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"SELECT * FROM ProductPhotos WHERE PhotoID = @PhotoID";

            var result = await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
            return result;
        }

        /// <summary>
        /// Lấy danh sách ảnh của mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Danh sách các ảnh</returns>
        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT * FROM ProductPhotos 
                WHERE ProductID = @ProductID 
                ORDER BY DisplayOrder";

            var result = await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID });
            return result.ToList();
        }

        /// <summary>
        /// Cập nhật ảnh của mặt hàng
        /// </summary>
        /// <param name="data">Dữ liệu ảnh cần cập nhật</param>
        /// <returns>True nếu thành công, False nếu thất bại</returns>
        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE ProductPhotos 
                SET Photo = @Photo,
                    Description = @Description,
                    DisplayOrder = @DisplayOrder,
                    IsHidden = @IsHidden
                WHERE PhotoID = @PhotoID";

            var result = await connection.ExecuteAsync(sql, data);
            return result > 0;
        }

        #endregion
    }
}