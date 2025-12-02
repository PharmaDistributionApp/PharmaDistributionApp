using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views
{
    public partial class LoginControl : UserControl
    {
        public LoginControl()
        {
            InitializeComponent();
        }

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
                    // Logic tìm kiếm thông minh (ID hoặc Email hoặc Mã NV)
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

                        // Mở màn hình chính
                        MainWindow main = new MainWindow();
                        main.Show();

                        // ĐÓNG CỬA SỔ CHA (LOGIN WINDOW)
                        // Lệnh này tìm cửa sổ đang chứa UserControl này và đóng nó lại
                        Window parentWindow = Window.GetWindow(this);
                        if (parentWindow != null)
                        {
                            parentWindow.Close();
                        }
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
    }
}