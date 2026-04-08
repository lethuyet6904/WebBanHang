using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.DataLayers.SQLServer;
using SV22T1020459.Models.Catalog;
using SV22T1020459.Models.Common;

namespace SV22T1020459.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến danh mục hàng hóa của hệ thống, 
    /// bao gồm: mặt hàng (Product), thuộc tính của mặt hàng (ProductAttribute) và ảnh của mặt hàng (ProductPhoto).
    /// </summary>
    public static class CatalogDataService
    {
        private static readonly IProductRepository productDB;
        private static readonly IGenericRepository<Category> categoryDB;

        /// <summary>
        /// Constructor tĩnh khởi tạo kết nối Database
        /// </summary>
        static CatalogDataService()
        {
            categoryDB = new CategoryRepository(Configuration.ConnectionString);
            productDB = new ProductRepository(Configuration.ConnectionString);
        }

        #region Category

        public static async Task<PagedResult<Category>> ListCategoriesAsync(PaginationSearchInput input)
        {
            try { return await categoryDB.ListAsync(input); }
            catch (Exception ex) { throw new Exception("Lỗi khi tải danh sách Loại hàng: " + ex.Message); }
        }

        public static async Task<Category?> GetCategoryAsync(int CategoryID)
        {
            try { return await categoryDB.GetAsync(CategoryID); }
            catch { return null; }
        }

        public static async Task<int> AddCategoryAsync(Category data)
        {
            try { return await categoryDB.AddAsync(data); }
            catch { return 0; }
        }

        public static async Task<bool> UpdateCategoryAsync(Category data)
        {
            try { return await categoryDB.UpdateAsync(data); }
            catch { return false; }
        }

        public static async Task<bool> DeleteCategoryAsync(int CategoryID)
        {
            try
            {
                if (await categoryDB.IsUsedAsync(CategoryID)) return false;
                return await categoryDB.DeleteAsync(CategoryID);
            }
            catch { return false; }
        }

        public static async Task<bool> IsUsedCategoryAsync(int CategoryID)
        {
            try { return await categoryDB.IsUsedAsync(CategoryID); }
            catch { return true; } // Báo True (đang dùng) để chặn không cho xóa nếu bị lỗi kết nối DB
        }

        #endregion

        #region Product

        public static async Task<PagedResult<Product>> ListProductsAsync(ProductSearchInput input)
        {
            try { return await productDB.ListAsync(input); }
            catch (Exception ex) { throw new Exception("Lỗi khi tải danh sách Mặt hàng: " + ex.Message); }
        }

        public static async Task<Product?> GetProductAsync(int productID)
        {
            try { return await productDB.GetAsync(productID); }
            catch { return null; }
        }

        public static async Task<int> AddProductAsync(Product data)
        {
            try { return await productDB.AddAsync(data); }
            catch { return 0; }
        }

        public static async Task<bool> UpdateProductAsync(Product data)
        {
            try { return await productDB.UpdateAsync(data); }
            catch { return false; }
        }

        public static async Task<bool> DeleteProductAsync(int productID)
        {
            try
            {
                if (await productDB.IsUsedAsync(productID)) return false;
                return await productDB.DeleteAsync(productID);
            }
            catch { return false; }
        }

        public static async Task<bool> IsUsedProductAsync(int productID)
        {
            try { return await productDB.IsUsedAsync(productID); }
            catch { return true; }
        }

        #endregion

        #region ProductAttribute

        public static async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            try { return await productDB.ListAttributesAsync(productID); }
            catch { return new List<ProductAttribute>(); }
        }

        public static async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            try { return await productDB.GetAttributeAsync(attributeID); }
            catch { return null; }
        }

        public static async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            try { return await productDB.AddAttributeAsync(data); }
            catch { return 0; }
        }

        public static async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            try { return await productDB.UpdateAttributeAsync(data); }
            catch { return false; }
        }

        public static async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            try { return await productDB.DeleteAttributeAsync(attributeID); }
            catch { return false; }
        }

        #endregion

        #region ProductPhoto

        public static async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            try { return await productDB.ListPhotosAsync(productID); }
            catch { return new List<ProductPhoto>(); }
        }

        public static async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            try { return await productDB.GetPhotoAsync(photoID); }
            catch { return null; }
        }

        public static async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            try { return await productDB.AddPhotoAsync(data); }
            catch { return 0; }
        }

        public static async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            try { return await productDB.UpdatePhotoAsync(data); }
            catch { return false; }
        }

        public static async Task<bool> DeletePhotoAsync(long photoID)
        {
            try { return await productDB.DeletePhotoAsync(photoID); }
            catch { return false; }
        }

        #endregion
    }
}