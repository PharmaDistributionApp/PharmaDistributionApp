using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Services
{
    public static class UserSession
    {
        // Biến này lưu thông tin người đang đăng nhập
        public static Employee CurrentUser { get; set; }

        // Biến kiểm tra trạng thái đăng nhập
        public static bool IsLoggedIn { get; set; } = false;

        // Hàm xóa dữ liệu khi đăng xuất
        public static void Clear()
        {
            CurrentUser = null;
            IsLoggedIn = false;
        }
    }
}