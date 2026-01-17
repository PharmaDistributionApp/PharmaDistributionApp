using PharmaDistributionApp.Models;

namespace PharmaDistributionApp
{
    // Class này dùng để lưu trữ thông tin người đang đăng nhập hiện tại
    public static class UserSession
    {
        // Biến static để lưu thông tin nhân viên
        public static Nhanvien CurrentUser { get; set; }

        // Hàm kiểm tra xem đã đăng nhập chưa
        public static bool IsLoggedIn { get; set; } = false;

        // Hàm đăng xuất
        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}