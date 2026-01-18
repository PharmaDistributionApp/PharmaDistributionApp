using PharmaDistributionApp.Services;
using PharmaDistributionApp.Views.EmployeeView; // Import namespace chứa EmployeeViewControl
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Views.LoginView;
using PharmaDistributionApp.Views.DashBoardView;
using PharmaDistributionApp.Views.NCCView;
using PharmaDistributionApp.Views.ProductView;
using PharmaDistributionApp.Views.Controls;

namespace PharmaDistributionApp.Views
{
    public partial class MainWindow : Window
    {
        public string CurrentMaNV { get; set; } // Biến lưu Mã NV đang đăng nhập
        public Employee CurrentUser { get; set; } // Biến lưu toàn bộ thông tin User
        public MainWindow()
        {
            InitializeComponent();

            // Hiển thị tên người dùng (Giả lập)
            txbUserName.Text = "Nguyễn Văn A";

            // Mặc định chọn Menu "Kho" khi mở lên (hoặc Nhân sự tùy bạn)
            SetActiveMenu(btnTongQuan);
            MainContent.Content = new DashBoardViewControl();
        }

        private void Menu_Click(object sender, MouseButtonEventArgs e)
        {
            // 1. Xác định nút vừa bấm
            var clickedBtn = sender as Border;
            if (clickedBtn == null) return;

            // 2. Đổi màu giao diện (Set Active)
            SetActiveMenu(clickedBtn);

            // 3. Chuyển đổi màn hình nội dung
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
                    // MainContent.Content = new WarehouseControl(); 
                    MainContent.Content = new KhoHangControl();
                    break;
                case "HoaDon":
                    MainContent.Content = new HoaDonControl();
                    break;
                case "NhaCungCap":
                    MainContent.Content = new NhaCungCapControl();
                    break;
                case "KhachHang":
                    // MainContent.Content = new KhachHangControl(); 
                    // Lưu ý: Bạn cần tạo File KhachHangControl.xaml trước khi bỏ comment dòng trên
                    MainContent.Content = new KhachHangControl();
                    break;
                // [MỚI] CASE XỬ LÝ SẢN PHẨM
                case "SanPham":
                    // Nếu bạn đã có UserControl cho sản phẩm thì thay dòng dưới bằng: new SanPhamControl();
                    MainContent.Content = new SanPhamControl();
                    break;

                case "Account":
                    MainContent.Content = new AccountControl();
                    break;
            }
        }

        // Hàm xử lý đổi màu nút Menu
        // Hàm xử lý đổi màu nút Menu
        private void SetActiveMenu(Border activeBtn)
        {
            // 1. Reset tất cả các nút về màu trong suốt, chữ trắng
            ResetButtonStyle(btnTongQuan);
            ResetButtonStyle(btnKho);
            ResetButtonStyle(btnNhanSu);
            ResetButtonStyle(btnHoaDon);
            ResetButtonStyle(btnNhaCungCap);

            // BỔ SUNG: Reset nút Khách hàng để không bị kẹt màu trắng
            ResetButtonStyle(btnKhachHang);

            ResetButtonStyle(btnSanPham);
            ResetButtonStyle(btnAccount);

            // 2. Set nút đang được chọn thành nền Trắng
            activeBtn.Background = Brushes.White;

            // 3. Tìm icon và text bên trong nút được chọn để đổi sang màu xanh thương hiệu #4C70BA
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
            // Xử lý đăng xuất (Ví dụ: Mở lại LoginWindow)
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}