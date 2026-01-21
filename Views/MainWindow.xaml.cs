using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Services;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Views.DashBoardView;
using PharmaDistributionApp.Views.EmployeeView;
using PharmaDistributionApp.Views.LoginView;
using PharmaDistributionApp.Views.NCCView;
using PharmaDistributionApp.Views.ProductView;
using PharmaDistributionApp.Views.Controls;
using PharmaDistributionApp.Views.KhachHangView;

namespace PharmaDistributionApp.Views
{
    public partial class MainWindow : Window
    {
        public Employee CurrentUser { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            if (UserSession.IsLoggedIn && UserSession.CurrentUser != null)
            {
                this.CurrentUser = UserSession.CurrentUser;
            }
            else
            {
                this.CurrentUser = new Employee { Tennv = "Admin (Debug)", Chucvu = "Quản lý" };
            }

            LoadUserData();
            SetActiveMenu(btnTongQuan);
            MainContent.Content = new DashBoardViewControl();
        }

        public void LoadUserData()
        {
            var user = UserSession.CurrentUser ?? this.CurrentUser;
            if (user == null) return;

            txbUserName.Text = !string.IsNullOrEmpty(user.Tennv) ? user.Tennv : "Người dùng";
            txbUserRole.Text = !string.IsNullOrEmpty(user.Chucvu) ? user.Chucvu : "Nhân viên";

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
            UserSession.Clear();
            new LoginWindow().Show();
            this.Close();
        }
    }
}