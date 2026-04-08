using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020459.DataLayers.Interfaces;
using SV22T1020459.Models.DataDictionary;

namespace SV22T1020459.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho Tỉnh/Thành (Province) trên CSDL SQL Server
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL SQL Server</param>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ các tỉnh/thành
        /// </summary>
        /// <returns>Danh sách các đối tượng Province</returns>
        public async Task<List<Province>> ListAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            // Truy vấn lấy danh sách tỉnh thành và sắp xếp theo tên (A-Z) để dễ hiển thị trên UI
            string sql = @"
                SELECT ProvinceName 
                FROM Provinces 
                ORDER BY ProvinceName";

            var dataItems = await connection.QueryAsync<Province>(sql);
            return dataItems.ToList();
        }
    }
}