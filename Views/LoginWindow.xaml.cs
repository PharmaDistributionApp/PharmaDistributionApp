using System.Linq;
using System.Windows;
using PharmaDistributionApp.Models;
using System.Windows.Media;

namespace PharmaDistributionApp.Views
{
    public partial class LoginWindow : Window
    {
        private Brush defaultBorderBrush = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            txbErrorMessage.Visibility = Visibility.Collapsed;
            string inputID = txtUsername.Text;
            string pass = txtPassword.Password;
            bool hasError = false;
            txbErrorMessage.Text = "Vui lòng nhập đầy đủ thông tin";

            if (string.IsNullOrEmpty(inputID))
            {
                txtUsername.BorderBrush = Brushes.Red;
                hasError = true;
                txbErrorMessage.Visibility = Visibility.Visible;

            }

            if (string.IsNullOrEmpty(pass))
            {
                txtPassword.BorderBrush = Brushes.Red;
                hasError = true;
                txbErrorMessage.Visibility = Visibility.Visible;
            }

            if (hasError) return;

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

        private void TextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PharmaDistributionApp.Views.ForgotPasswordWindow p = new PharmaDistributionApp.Views.ForgotPasswordWindow();
            p.Show();
            this.Close();
        }

        private void txtUsername_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (txtUsername.BorderBrush == Brushes.Red)
            {
                txtUsername.BorderBrush = defaultBorderBrush;
            }
            txbErrorMessage.Visibility = Visibility.Collapsed;
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtPassword.BorderBrush == Brushes.Red)
            {
                txtPassword.BorderBrush = defaultBorderBrush;
            }
            txbErrorMessage.Visibility = Visibility.Collapsed;
        }
    }
}