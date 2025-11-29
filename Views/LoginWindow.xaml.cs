using System.Linq;
using System.Windows;
using PharmaDistributionApp.Models; // Đảm bảo dòng này đúng tên Project của bạn

namespace PharmaDistributionApp.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Lấy dữ liệu
            string id = txtUsername.Text;
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Vui lòng nhập ID và Mật khẩu!", "Thông báo");
                return;
            }

            // Kết nối CSDL
            try
            {
                // Lưu ý: Tên Context phải đúng với file trong thư mục Models
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // Tìm user có ID và Password khớp
                    var user = context.Taikhoans
                                      .Where(u => u.Tentk == id && u.Matkhau == pass)
                                      .FirstOrDefault();

                    if (user != null)
                    {
                        if (user.Trangthai == 0)
                        {
                            MessageBox.Show("Tài khoản đã bị khóa!", "Lỗi");
                            return;
                        }

                        // Mở màn hình chính
                        MainWindow main = new MainWindow();
                        main.Show();

                        // Đóng màn hình đăng nhập
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Sai ID hoặc Mật khẩu!", "Lỗi đăng nhập");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message, "Lỗi hệ thống");
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}