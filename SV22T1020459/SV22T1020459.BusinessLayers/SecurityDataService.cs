using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.DataLayers.SQLServer;
using SV22T1020459.Models.Security;
using System;
using System.Threading.Tasks;

namespace SV22T1020459.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý liên quan đến bảo mật và tài khoản
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository customerAccountDB;
        private static readonly IUserAccountRepository employeeAccountDB;

        /// <summary>
        /// Constructor tĩnh khởi tạo kết nối Database
        /// </summary>
        static SecurityDataService()
        {
            customerAccountDB = new CustomerAccountRepository(Configuration.ConnectionString);
            employeeAccountDB = new EmployeeAccountRepository(Configuration.ConnectionString);
        }

        #region Xử lý tài khoản Khách hàng (Customer)

        /// <summary>
        /// Xác thực tài khoản khách hàng
        /// </summary>
        public static async Task<UserAccount?> AuthorizeCustomerAsync(string userName, string password)
        {
            try
            {
                return await customerAccountDB.AuthorizeAsync(userName, password);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Đổi mật khẩu khách hàng
        /// </summary>
        public static async Task<bool> ChangePasswordCustomerAsync(string userName, string password)
        {
            try
            {
                return await customerAccountDB.ChangePasswordAsync(userName, password);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Xử lý tài khoản Nhân viên (Employee)

        /// <summary>
        /// Xác thực tài khoản nhân viên
        /// </summary>
        public static async Task<UserAccount?> AuthorizeEmployeeAsync(string userName, string password)
        {
            try
            {
                return await employeeAccountDB.AuthorizeAsync(userName, password);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        public static async Task<bool> ChangePasswordEmployeeAsync(string userName, string password)
        {
            try
            {
                return await employeeAccountDB.ChangePasswordAsync(userName, password);
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}