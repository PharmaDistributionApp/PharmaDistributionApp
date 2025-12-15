using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Views; // Để gọi AccountControl

namespace PharmaDistributionApp
{
    public partial class MainWindow : Window
    {
        public static string CurrentMaNV { get; set; } = "NV001";

        // Khai báo màu sắc
        private readonly Brush _activeBackground = Brushes.White;
        private readonly Brush _inactiveBackground = Brushes.Transparent;
        private readonly Brush _activeForeground = (Brush)new BrushConverter().ConvertFrom("#4C70BA"); // Xanh
        private readonly Brush _inactiveForeground = Brushes.White;

        public MainWindow()
        {
            InitializeComponent();
            LoadSidebarInfo();

            // Mặc định chọn tab Tài khoản khi mở lên
            SwitchView("Account");
        }

        private void LoadSidebarInfo()
        {
            // ... (Code cũ giữ nguyên) ...
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var nv = context.Nhanviens.FirstOrDefault(x => x.Manv == CurrentMaNV);
                    if (nv != null)
                    {
                        txbUserName.Text = nv.Tennv;
                        if (!string.IsNullOrEmpty(nv.Chucvu)) txbUserRole.Text = nv.Chucvu;
                    }
                }
            }
            catch { txbUserName.Text = "Offline"; }
        }

        // --- XỬ LÝ SỰ KIỆN CLICK MENU ---
        private void Menu_Click(object sender, MouseButtonEventArgs e)
        {
            var clickedBorder = sender as Border;
            if (clickedBorder == null) return;

            // 1. Reset màu tất cả menu về trạng thái chưa chọn
            ResetMenuVisuals();

            // 2. Highlight menu vừa click
            HighlightMenu(clickedBorder);

            // 3. Chuyển màn hình
            string tag = clickedBorder.Tag.ToString();
            SwitchView(tag);
        }

        private void SwitchView(string viewName)
        {
            switch (viewName)
            {
                case "Account":
                    MainContent.Content = new AccountControl();
                    break;

                // Vì bạn chưa có các UserControl khác nên tôi dùng tạm TextBlock để demo
                // Sau này bạn tạo file Views/KhoControl.xaml thì thay thế vào đây: new KhoControl();
                case "Kho":
                    MainContent.Content = new TextBlock { Text = "Màn hình Kho", FontSize = 30, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    break;
                case "NhanSu":
                    MainContent.Content = new TextBlock { Text = "Màn hình Nhân sự", FontSize = 30, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    break;
                case "HoaDon":
                    MainContent.Content = new TextBlock { Text = "Màn hình Hóa đơn", FontSize = 30, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    break;
                case "NhaCungCap":
                    MainContent.Content = new TextBlock { Text = "Màn hình Nhà cung cấp", FontSize = 30, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    break;
            }
        }

        // --- CÁC HÀM HỖ TRỢ ĐỔI MÀU GIAO DIỆN ---

        private void ResetMenuVisuals()
        {
            // Danh sách các nút menu
            var menus = new Border[] { btnAccount, btnKho, btnNhanSu, btnHoaDon, btnNhaCungCap };

            foreach (var border in menus)
            {
                border.Background = _inactiveBackground;

                // Lấy StackPanel bên trong Border
                var stack = border.Child as StackPanel;
                if (stack != null)
                {
                    // Phần tử 0 là Icon, 1 là TextBlock
                    var icon = stack.Children[0] as PackIcon;
                    var text = stack.Children[1] as TextBlock;

                    if (icon != null) icon.Foreground = _inactiveForeground;
                    if (text != null) text.Foreground = _inactiveForeground;
                    if (text != null) text.FontWeight = FontWeights.Normal;
                }
            }
        }

        private void HighlightMenu(Border border)
        {
            border.Background = _activeBackground;

            var stack = border.Child as StackPanel;
            if (stack != null)
            {
                var icon = stack.Children[0] as PackIcon;
                var text = stack.Children[1] as TextBlock;

                if (icon != null) icon.Foreground = _activeForeground;
                if (text != null) text.Foreground = _activeForeground;
                if (text != null) text.FontWeight = FontWeights.Bold;
            }
        }
    }
}