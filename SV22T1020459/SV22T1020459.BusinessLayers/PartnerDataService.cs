using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.DataLayers.SQLServer;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.Partner;
using System;
using System.Threading.Tasks;

namespace SV22T1020459.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến các đối tác của hệ thống
    /// bao gồm: nhà cung cấp (Supplier), khách hàng (Customer) và người giao hàng (Shipper)
    /// </summary>
    public static class PartnerDataService
    {
        private static readonly IGenericRepository<Supplier> supplierDB;
        private static readonly ICustomerRepository customerDB;
        private static readonly IGenericRepository<Shipper> shipperDB;

        /// <summary>
        /// Khởi tạo kết nối DB
        /// </summary>
        static PartnerDataService()
        {
            supplierDB = new SupplierRepository(Configuration.ConnectionString);
            customerDB = new CustomerRepository(Configuration.ConnectionString);
            shipperDB = new ShipperRepository(Configuration.ConnectionString);
        }

        #region Supplier

        public static async Task<PagedResult<Supplier>> ListSuppliersAsync(PaginationSearchInput input)
        {
            try { return await supplierDB.ListAsync(input); }
            catch (Exception ex) { throw new Exception("Lỗi khi tải danh sách Nhà cung cấp: " + ex.Message); }
        }

        public static async Task<Supplier?> GetSupplierAsync(int supplierID)
        {
            try { return await supplierDB.GetAsync(supplierID); }
            catch { return null; }
        }

        public static async Task<int> AddSupplierAsync(Supplier data)
        {
            try { return await supplierDB.AddAsync(data); }
            catch { return 0; }
        }

        public static async Task<bool> UpdateSupplierAsync(Supplier data)
        {
            try { return await supplierDB.UpdateAsync(data); }
            catch { return false; }
        }

        public static async Task<bool> DeleteSupplierAsync(int supplierID)
        {
            try
            {
                if (await supplierDB.IsUsedAsync(supplierID))
                    return false;
                return await supplierDB.DeleteAsync(supplierID);
            }
            catch { return false; }
        }

        public static async Task<bool> IsUsedSupplierAsync(int supplierID)
        {
            try { return await supplierDB.IsUsedAsync(supplierID); }
            catch { return true; } // Lỗi mạng thì chặn xóa cho an toàn
        }

        /// <summary>
        /// SỬA LỖI: Kiểm tra email Nhà cung cấp hợp lệ 
        /// </summary>
        public static async Task<bool> ValidateSupplierEmailAsync(string email, int supplierID = 0)
        {
            try
            {
                // Cần ép kiểu IGenericRepository sang interface riêng nếu có hàm ValidateEmailAsync, 
                // HOẶC trong SupplierRepository phải có hàm tương tự CustomerRepository
                // Dưới đây giả sử SupplierRepository có phương thức ValidateEmailAsync (Nếu chưa có ông cháu nhớ thêm vào SupplierRepository nhé)
                var repo = supplierDB as SupplierRepository;
                if (repo != null)
                    return await repo.ValidateEmailAsync(email, supplierID);

                return false;
            }
            catch { return false; }
        }

        #endregion

        #region Customer

        public static async Task<PagedResult<Customer>> ListCustomersAsync(PaginationSearchInput input)
        {
            try { return await customerDB.ListAsync(input); }
            catch (Exception ex) { throw new Exception("Lỗi khi tải danh sách Khách hàng: " + ex.Message); }
        }

        public static async Task<Customer?> GetCustomerAsync(int customerID)
        {
            try { return await customerDB.GetAsync(customerID); }
            catch { return null; }
        }

        public static async Task<int> AddCustomerAsync(Customer data)
        {
            try { return await customerDB.AddAsync(data); }
            catch { return 0; }
        }

        public static async Task<bool> UpdateCustomerAsync(Customer data)
        {
            try { return await customerDB.UpdateAsync(data); }
            catch { return false; }
        }

        public static async Task<bool> DeleteCustomerAsync(int customerID)
        {
            try
            {
                if (await customerDB.IsUsedAsync(customerID))
                    return false;
                return await customerDB.DeleteAsync(customerID);
            }
            catch { return false; }
        }

        public static async Task<bool> IsUsedCustomerAsync(int customerID)
        {
            try { return await customerDB.IsUsedAsync(customerID); }
            catch { return true; }
        }

        public static async Task<bool> ValidatelCustomerEmailAsync(string email, int customerID = 0)
        {
            try { return await customerDB.ValidateEmailAsync(email, customerID); }
            catch { return false; }
        }

        #endregion

        #region Shipper

        public static async Task<PagedResult<Shipper>> ListShippersAsync(PaginationSearchInput input)
        {
            try { return await shipperDB.ListAsync(input); }
            catch (Exception ex) { throw new Exception("Lỗi khi tải danh sách Người giao hàng: " + ex.Message); }
        }

        public static async Task<Shipper?> GetShipperAsync(int shipperID)
        {
            try { return await shipperDB.GetAsync(shipperID); }
            catch { return null; }
        }

        public static async Task<int> AddShipperAsync(Shipper data)
        {
            try { return await shipperDB.AddAsync(data); }
            catch { return 0; }
        }

        public static async Task<bool> UpdateShipperAsync(Shipper data)
        {
            try { return await shipperDB.UpdateAsync(data); }
            catch { return false; }
        }

        public static async Task<bool> DeleteShipperAsync(int shipperID)
        {
            try
            {
                if (await shipperDB.IsUsedAsync(shipperID))
                    return false;
                return await shipperDB.DeleteAsync(shipperID);
            }
            catch { return false; }
        }

        public static async Task<bool> IsUsedShipperAsync(int shipperID)
        {
            try { return await shipperDB.IsUsedAsync(shipperID); }
            catch { return true; }
        }

        #endregion
    }
}