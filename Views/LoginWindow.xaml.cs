using System.Linq;
using System.Windows;
using PharmaDistributionApp.Models; // Đảm bảo đúng namespace

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
            txbErrorMessage.Visibility = Visibility.Collapsed;
            string inputID = txtUsername.Text;
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(inputID) || string.IsNullOrEmpty(pass))
            {
                txbErrorMessage.Text = "Vui lòng nhập đầy đủ thông tin";
                txbErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var user = context.Taikhoans.FirstOrDefault(u => u.Tentk == inputID && u.Matkhau == pass);

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
                        txbErrorMessage.Text = "Sai tài khoản hoặc mật khẩu";
                        txbErrorMessage.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}