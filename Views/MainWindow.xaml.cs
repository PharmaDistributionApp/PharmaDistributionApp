using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Services;
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
        // Biến lưu thông tin người dùng hiện tại
        public Employee CurrentUser { get; set; }

        // SỬA LỖI: Thuộc tính này lấy trực tiếp từ CurrentUser để các tab khác (AccountControl) không bị lỗi
        public string CurrentMaNV => CurrentUser?.Manv;

        // Constructor nhận tham số Employee từ màn hình Đăng nhập
        public MainWindow(Employee user)
        {
            InitializeComponent();

            // Lưu user vào biến toàn cục và đồng bộ vào Session để các tab con dùng chung
            this.CurrentUser = user;
            UserSession.CurrentUser = user;

            // Hiển thị thông tin lên Header góc trái
            LoadUserData();

            // Mặc định chọn Menu Tổng quan
            SetActiveMenu(btnTongQuan);
            MainContent.Content = new DashBoardViewControl();
        }

        public MainWindow() : this(null) { }

        private void LoadUserData()
        {
            if (CurrentUser == null) return;

            // 1. Gán Tên và Chức vụ thực tế
            txbUserName.Text = !string.IsNullOrEmpty(CurrentUser.Tennv) ? CurrentUser.Tennv : "Người dùng";
            txbUserRole.Text = !string.IsNullOrEmpty(CurrentUser.Chucvu) ? CurrentUser.Chucvu : "Nhân viên";

            // 2. Xử lý Avatar bằng thuộc tính AvatarSource có sẵn
            if (CurrentUser.AvatarSource != null)
            {
                imgAvatarBrush.ImageSource = CurrentUser.AvatarSource;
                iconAvatar.Visibility = Visibility.Collapsed; // Ẩn icon mặc định
            }
            else
            {
                imgAvatarBrush.ImageSource = null;
                iconAvatar.Visibility = Visibility.Visible; // Hiện icon mặc định
            }
        }

        // --- Logic chuyển đổi Menu (Giữ nguyên các tính năng cũ) ---
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
            ResetButtonStyle(btnTongQuan);
            ResetButtonStyle(btnKho);
            ResetButtonStyle(btnNhanSu);
            ResetButtonStyle(btnHoaDon);
            ResetButtonStyle(btnNhaCungCap);
            ResetButtonStyle(btnKhachHang);
            ResetButtonStyle(btnSanPham);
            ResetButtonStyle(btnAccount);

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
            new LoginWindow().Show();
            this.Close();
        }
    }
}