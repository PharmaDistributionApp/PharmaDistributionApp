using System.Linq;
using System.Windows;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // Xử lý nút Đăng nhập
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            txbErrorMessage.Visibility = Visibility.Collapsed;
            string input = txtUsername.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pass))
            {
                txbErrorMessage.Text = "Vui lòng nhập đầy đủ thông tin";
                txbErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // Tìm user bằng LINQ JOIN (như đã sửa trước đó)
                    var user = (from tk in context.Taikhoans
                                join nv in context.Nhanviens on tk.Manv equals nv.Manv
                                where (tk.Tentk == input || tk.Manv == input || nv.Email == input)
                                      && tk.Matkhau == pass
                                select tk).FirstOrDefault();

                    if (user != null)
                    {
                        if (user.Trangthai == 0)
                        {
                            txbErrorMessage.Text = "Tài khoản đã bị khóa!";
                            txbErrorMessage.Visibility = Visibility.Visible;
                            return;
                        }

                        MainWindow main = new MainWindow();
                        main.Show();
                        this.Close();
                    }
                    else
                    {
                        txbErrorMessage.Text = "Sai thông tin đăng nhập hoặc mật khẩu";
                        txbErrorMessage.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        // --- CÁC NÚT ĐIỀU KHIỂN CỬA SỔ ---

        // 1. Nút Thoát (Close)
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // 2. Nút Thu nhỏ (Minimize)
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 3. Nút Phóng to (Maximize)
        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }
    }
}