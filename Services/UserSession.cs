using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;

namespace PharmaDistributionApp
{
    // Class này dùng để lưu trữ thông tin người đang đăng nhập hiện tại
    public static class UserSession
    {
        // Biến static để lưu thông tin nhân viên
        public static Employee CurrentUser { get; set; }
        public static bool IsLoggedIn { get; set; }

        // Hàm đăng xuất
        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}