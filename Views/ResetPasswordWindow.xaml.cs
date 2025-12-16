using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PharmaDistributionApp.Services;
using System.Data.SQLite;

namespace PharmaDistributionApp.Views
{
    // 1. Kế thừa UserControl
    public partial class ResetPasswordWindow : UserControl
    {
        private string _userEmail;
        private Brush defaultBorder = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");

        public ResetPasswordWindow(string userEmail = "")
        {
            InitializeComponent();
            _userEmail = userEmail;
        }

        // Constructor mặc định
        public ResetPasswordWindow()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Reset màu về mặc định trước khi kiểm tra
            txtNewPassword.BorderBrush = defaultBorder;
            txtConfirmPassword.BorderBrush = defaultBorder;

            string newPass = txtNewPassword.Password;
            string confirmPass = txtConfirmPassword.Password;
            bool hasError = false;

            // Kiểm tra rỗng
            if (string.IsNullOrEmpty(newPass))
            {
                txtNewPassword.BorderBrush = Brushes.Red;
                hasError = true;
            }

            if (string.IsNullOrEmpty(confirmPass))
            {
                txtConfirmPassword.BorderBrush = Brushes.Red;
                hasError = true;
            }

            if (hasError) return;

            // Kiểm tra khớp nhau
            if (newPass != confirmPass)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                txtConfirmPassword.BorderBrush = Brushes.Red;
                txtConfirmPassword.Clear();
                txtConfirmPassword.Focus();
                return;
            }

            // -- CẬP NHẬP MỚI VÀO DATABASE --
            try
            {
                string sql = @"UPDATE TAIKHOAN
                             SET MatKhau = @pass
                             WHERE MANV = (SELECT MANV FROM NHANVIEN WHERE EMAIL = @email)";

                SQLiteParameter[] p = 
                {
                    new SQLiteParameter("@pass", newPass),
                    new SQLiteParameter("@email", _userEmail)
                };

                int rows = Database.ExecuteNonQuery(sql, p);

                if (rows > 0)
                {
                    MessageBox.Show("Đặt lại mật khẩu thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    var parentWindow = Window.GetWindow(this) as LoginWindow;
                    if (parentWindow != null)
                    {
                        parentWindow.NavigateToLogin();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đặt lại mật khẩu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Các hàm xử lý giao diện (đổi màu viền khi nhập lại)
        private void txtNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtNewPassword.BorderBrush == Brushes.Red)
                txtNewPassword.BorderBrush = defaultBorder;
        }

        private void txtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtConfirmPassword.BorderBrush == Brushes.Red)
                txtConfirmPassword.BorderBrush = defaultBorder;
        }
    }
}