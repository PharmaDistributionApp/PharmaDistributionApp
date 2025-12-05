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
                    // LOGIC TÌM KIẾM ĐA NĂNG:
                    var user = (from tk in context.Taikhoans
                                join nv in context.Nhanviens on tk.Manv equals nv.Manv
                                where (tk.Manv == input ||    // Trùng Mã NV
                                       tk.Tentk == input ||   // Trùng Tên TK
                                       nv.Email == input)     // Trùng Email
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

                        // Đăng nhập thành công
                        MainWindow main = new MainWindow();
                        main.Show();

                        Window parent = Window.GetWindow(this);
                        if (parent != null) parent.Close();
                    }
                    else
                    {
                        txbErrorMessage.Text = "Sai tên tài khoản/mã nhân viên hoặc mật khẩu";
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