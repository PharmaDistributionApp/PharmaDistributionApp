namespace PharmaDistributionApp.Models
{
    // Class tĩnh: Dữ liệu sẽ tồn tại xuyên suốt chừng nào phần mềm còn chạy
    public static class UserSession
    {
        // Biến lưu mã nhân viên hiện tại
        public static string CurrentMaNV { get; set; }

        // Hàm xóa session khi đăng xuất
        public static void Clear()
        {
            CurrentMaNV = null;
        }
    }
}