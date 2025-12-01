using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PharmaDistributionApp.Views
{
    /// <summary>
    /// Interaction logic for ChangePasswordWindow.xaml
    /// </summary>
    public partial class ResetPasswordWindow : Window
    {
        private string _userEmail;
        private Brush defaultBorder = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");
        public ResetPasswordWindow(string userEmail = "")
        {
            InitializeComponent();
            _userEmail = userEmail;
        }

       private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Reset màu về mặc định trước khi kiểm tra
            txtNewPassword.BorderBrush = defaultBorder;
            txtConfirmPassword.BorderBrush = defaultBorder;

            string newPass = txtNewPassword.Password;
            string confirmPass = txtConfirmPassword.Password;
            bool hasError = false;

            // Kiểm tra rỗng: Mật khẩu mới
            if (string.IsNullOrEmpty(newPass))
            {
                txtNewPassword.BorderBrush = Brushes.Red;
                hasError = true;
            }

            // Kiểm tra rỗng: Xác nhận mật khẩu
            if (string.IsNullOrEmpty(confirmPass))
            {
                txtConfirmPassword.BorderBrush = Brushes.Red;
                hasError = true;
            }

            if (hasError) return; // Nếu có ô trống thì dừng luôn

            // Kiểm tra khớp nhau
            if (newPass != confirmPass)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Bôi đỏ ô xác nhận để người dùng biết chỗ sai
                txtConfirmPassword.BorderBrush = Brushes.Red;
                
                // Xóa nội dung ô xác nhận đi
                txtConfirmPassword.Clear();
                txtConfirmPassword.Focus(); 
                return;
            }

            // --- NẾU MỌI THỨ OK ---
            // Code lưu vào Database ở đây...
            
            MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo");
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
       }
        private void txtNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtNewPassword.BorderBrush == Brushes.Red)
            {
                txtNewPassword.BorderBrush = defaultBorder;
            }
        }

        private void txtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtConfirmPassword.BorderBrush == Brushes.Red)
            {
                txtConfirmPassword.BorderBrush = defaultBorder;
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
