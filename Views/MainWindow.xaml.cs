using PharmaDistributionApp.Services; // Chứa class Employee
using PharmaDistributionApp.Views.Controls;
using PharmaDistributionApp.Views.DashBoardView;
using PharmaDistributionApp.Views.EmployeeView;
using PharmaDistributionApp.Views.LoginView;
using PharmaDistributionApp.Views.NCCView;
using PharmaDistributionApp.Views.ProductView;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PharmaDistributionApp.Views
{
    public partial class MainWindow : Window
    {
        // Biến lưu thông tin người dùng hiện tại
        public Employee CurrentUser { get; set; }

        // [SỬA ĐỔI] Constructor nhận tham số Employee từ màn hình Đăng nhập
        public MainWindow(Employee user)
        {
            InitializeComponent();

            // Lưu user vào biến toàn cục của cửa sổ
            this.CurrentUser = user;

            // Gọi hàm hiển thị thông tin lên góc trái
            LoadUserData();

            // Mặc định chọn Menu Tổng quan
            SetActiveMenu(btnTongQuan);
            MainContent.Content = new DashBoardViewControl();
        }

        // Constructor mặc định (để tránh lỗi nếu gọi new MainWindow() không tham số)
        public MainWindow()
        {
            InitializeComponent();
            SetActiveMenu(btnTongQuan);
        }

        // ============================================================
        // HÀM HIỂN THỊ THÔNG TIN USER TỪ BIẾN CurrentUser
        // ============================================================
        private void LoadUserData()
        {
            if (CurrentUser == null) return;

            // 1. Gán Tên và Chức vụ
            txbUserName.Text = !string.IsNullOrEmpty(CurrentUser.Tennv) ? CurrentUser.Tennv : "Người dùng";
            txbUserRole.Text = !string.IsNullOrEmpty(CurrentUser.Chucvu) ? CurrentUser.Chucvu : "Nhân viên";

            // 2. Xử lý Avatar
            // Sử dụng thuộc tính AvatarSource có sẵn trong file Employee.cs của bạn
            var avatar = CurrentUser.AvatarSource;

            if (avatar != null)
            {
                imgAvatarBrush.ImageSource = avatar;    // Gán ảnh
                iconAvatar.Visibility = Visibility.Collapsed; // Ẩn icon mặc định
            }
            else
            {
                imgAvatarBrush.ImageSource = null;    // Xóa ảnh cũ (nếu có)
                iconAvatar.Visibility = Visibility.Visible; // Hiện icon mặc định
            }
        }

        // ============================================================
        // PHẦN DƯỚI GIỮ NGUYÊN (Xử lý Menu)
        // ============================================================
        private void Menu_Click(object sender, MouseButtonEventArgs e)
        {
            var clickedBtn = sender as Border;
            if (clickedBtn == null) return;

            SetActiveMenu(clickedBtn);

            string tag = clickedBtn.Tag.ToString();
            switch (tag)
            {
                case "TongQuan":
                    MainContent.Content = new DashBoardViewControl();
                    break;
                case "NhanSu":
                    MainContent.Content = new EmployeeViewControl();
                    break;
                case "Kho":
                    MainContent.Content = new TextBlock { Text = "Màn hình Kho đang phát triển", FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    break;
                case "HoaDon":
                    MainContent.Content = new HoaDonControl();
                    break;
                case "NhaCungCap":
                    MainContent.Content = new NhaCungCapControl();
                    break;
                case "KhachHang":
                    MainContent.Content = new TextBlock { Text = "" };
                    break;
                case "SanPham":
                    MainContent.Content = new TextBlock { Text = "Màn hình Sản phẩm đang phát triển", FontSize = 20, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    break;
                case "Account":
                    MainContent.Content = new AccountControl();
                    break;
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
                foreach (var child in sp.Children)
                {
                    if (child is MaterialDesignThemes.Wpf.PackIcon icon)
                        icon.Foreground = (Brush)new BrushConverter().ConvertFrom("#4C70BA");
                    if (child is TextBlock txt)
                        txt.Foreground = (Brush)new BrushConverter().ConvertFrom("#4C70BA");
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
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}