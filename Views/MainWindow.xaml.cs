using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Services; // Để dùng UserSession
using PharmaDistributionApp.Models;   // Để dùng Employee
using PharmaDistributionApp.Views.DashBoardView;
using PharmaDistributionApp.Views.EmployeeView;
using PharmaDistributionApp.Views.LoginView;
using PharmaDistributionApp.Views.NCCView;
using PharmaDistributionApp.Views.ProductView;
using PharmaDistributionApp.Views.Controls;

namespace PharmaDistributionApp.Views
{
    public partial class MainWindow : Window
    {
        // Biến lưu thông tin người dùng cục bộ (Chỉ để binding nếu cần)
        public Employee CurrentUser { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // [SỬA LỖI 1]: Lấy dữ liệu trực tiếp từ Session thay vì tham số
            // Điều này đảm bảo dù khởi tạo ở đâu cũng có dữ liệu đúng
            if (UserSession.IsLoggedIn && UserSession.CurrentUser != null)
            {
                this.CurrentUser = UserSession.CurrentUser;
            }
            else
            {
                // Nếu chưa đăng nhập (Debug mode), gán dữ liệu mẫu hoặc rỗng
                this.CurrentUser = new Employee { Tennv = "Admin (Debug)", Chucvu = "Quản lý" };
            }

            // Hiển thị thông tin lên Header
            LoadUserData();

            // Mặc định chọn Menu Tổng quan
            SetActiveMenu(btnTongQuan);
            MainContent.Content = new DashBoardViewControl();
        }

        // Hàm nạp dữ liệu lên Header
        public void LoadUserData()
        {
            // Luôn lấy từ Session mới nhất (phòng trường hợp vừa đổi Avatar)
            var user = UserSession.CurrentUser ?? this.CurrentUser;

            if (user == null) return;

            // 1. Gán Tên và Chức vụ
            txbUserName.Text = !string.IsNullOrEmpty(user.Tennv) ? user.Tennv : "Người dùng";
            txbUserRole.Text = !string.IsNullOrEmpty(user.Chucvu) ? user.Chucvu : "Nhân viên";

            // 2. Xử lý Avatar (Dùng property AvatarSource đã có trong Model)
            if (user.AvatarSource != null)
            {
                imgAvatarBrush.ImageSource = user.AvatarSource;
                if (iconAvatar != null) iconAvatar.Visibility = Visibility.Collapsed;
            }
            else
            {
                imgAvatarBrush.ImageSource = null;
                if (iconAvatar != null) iconAvatar.Visibility = Visibility.Visible;
            }
        }

        // --- CÁC HÀM XỬ LÝ MENU (GIỮ NGUYÊN) ---

        private void Menu_Click(object sender, MouseButtonEventArgs e)
        {
            var clickedBtn = sender as Border;
            if (clickedBtn == null) return;

            SetActiveMenu(clickedBtn);

            string tag = clickedBtn.Tag.ToString();
            switch (tag)
            {
                case "TongQuan": MainContent.Content = new DashBoardViewControl(); break;
                case "NhanSu": MainContent.Content = new EmployeeViewControl(); break;
                case "Kho": MainContent.Content = new KhoHangControl(); break;
                case "HoaDon": MainContent.Content = new HoaDonControl(); break;
                case "NhaCungCap": MainContent.Content = new NhaCungCapControl(); break;
                case "KhachHang": MainContent.Content = new KhachHangControl(); break;
                case "SanPham": MainContent.Content = new SanPhamControl(); break;
                case "Account": MainContent.Content = new AccountControl(); break;
            }
        }

        private void SetActiveMenu(Border activeBtn)
        {
            // Reset tất cả nút về trong suốt
            ResetButtonStyle(btnTongQuan);
            ResetButtonStyle(btnKho);
            ResetButtonStyle(btnNhanSu);
            ResetButtonStyle(btnHoaDon);
            ResetButtonStyle(btnNhaCungCap);
            ResetButtonStyle(btnKhachHang);
            ResetButtonStyle(btnSanPham);
            ResetButtonStyle(btnAccount);

            // Active nút được chọn
            activeBtn.Background = Brushes.White;

            if (activeBtn.Child is StackPanel sp)
            {
                var blueBrush = (Brush)new BrushConverter().ConvertFrom("#4C70BA");
                foreach (var child in sp.Children)
                {
                    if (child is MaterialDesignThemes.Wpf.PackIcon icon) icon.Foreground = blueBrush;
                    if (child is TextBlock txt) txt.Foreground = blueBrush;
                }
            }
        }

        private void ResetButtonStyle(Border btn)
        {
            btn.Background = Brushes.Transparent;
            if (btn.Child is StackPanel sp)
            {
                foreach (var child in sp.Children)
                {
                    if (child is MaterialDesignThemes.Wpf.PackIcon icon) icon.Foreground = Brushes.White;
                    if (child is TextBlock txt) txt.Foreground = Brushes.White;
                }
            }
        }

        private void btnLogOut_Click(object sender, RoutedEventArgs e)
        {
            // [QUAN TRỌNG]: Xóa session khi đăng xuất
            UserSession.Clear();

            new LoginWindow().Show();
            this.Close();
        }
    }
}