namespace SV22T1020459.Models.Security
{
    public class ChangePassword
    {
        /// <summary>
        /// Lấy mật khẩu cũ của người dùng
        /// </summary>
        public string oldPassword { get; set; } = string.Empty;

        /// <summary>
        /// Nhập mật khẩu mới
        /// </summary>
        public string newPassword { get; set; } = string.Empty;

        /// <summary>
        /// check password 
        /// </summary>
        public string confirmPassword { get; set; } = string.Empty;
    }
}
