using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.DataLayers.SQLServer;
using SV22T1020459.Models.Common;
using SV22T1020459.Models.HR;
using System;
using System.Threading.Tasks;

namespace SV22T1020459.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến nhân sự của hệ thống    
    /// </summary>
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        /// <summary>
        /// Constructor khởi tạo kết nối Database
        /// </summary>
        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        #region Employee

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên dưới dạng phân trang.
        /// </summary>
        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
        {
            try { return await employeeDB.ListAsync(input); }
            catch (Exception ex) { throw new Exception("Lỗi khi tải danh sách Nhân viên: " + ex.Message); }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên.
        /// </summary>
        public static async Task<Employee?> GetEmployeeAsync(int employeeID)
        {
            try { return await employeeDB.GetAsync(employeeID); }
            catch { return null; }
        }

        /// <summary>
        /// Bổ sung một nhân viên mới.
        /// </summary>
        public static async Task<int> AddEmployeeAsync(Employee data)
        {
            try { return await employeeDB.AddAsync(data); }
            catch { return 0; }
        }

        /// <summary>
        /// Cập nhật thông tin của một nhân viên.
        /// </summary>
        public static async Task<bool> UpdateEmployeeAsync(Employee data)
        {
            try { return await employeeDB.UpdateAsync(data); }
            catch { return false; }
        }

        /// <summary>
        /// Xóa một nhân viên.
        /// </summary>
        public static async Task<bool> DeleteEmployeeAsync(int employeeID)
        {
            try
            {
                if (await employeeDB.IsUsedAsync(employeeID))
                    return false;

                return await employeeDB.DeleteAsync(employeeID);
            }
            catch { return false; }
        }

        /// <summary>
        /// Kiểm tra xem nhân viên có đang được sử dụng không.
        /// </summary>
        public static async Task<bool> IsUsedEmployeeAsync(int employeeID)
        {
            try { return await employeeDB.IsUsedAsync(employeeID); }
            catch { return true; }
        }

        /// <summary>
        /// Kiểm tra email có hợp lệ không.
        /// </summary>
        public static async Task<bool> ValidateEmployeeEmailAsync(string email, int employeeID = 0)
        {
            try { return await employeeDB.ValidateEmailAsync(email, employeeID); }
            catch { return false; }
        }

        /// <summary>
        /// Cập nhật quyền hạn (Role) của nhân viên.
        /// </summary>
        public static async Task<bool> UpdateEmployeeRoleAsync(int employeeID, string roleNames)
        {
            try { return await employeeDB.UpdateRoleAsync(employeeID, roleNames); }
            catch { return false; }
        }

        #endregion
    }
}