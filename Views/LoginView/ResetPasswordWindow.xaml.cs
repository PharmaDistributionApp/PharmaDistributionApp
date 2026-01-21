using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using PharmaDistributionApp.Services;

namespace PharmaDistributionApp.Views.LoginView
{
    public partial class ResetPasswordWindow : UserControl
    {
        private string _userEmail;
        private bool _isSystemClearing = false;

        public ResetPasswordWindow(string userEmail = "")
        {
            InitializeComponent();
            _userEmail = userEmail;
        }

        public ResetPasswordWindow()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

            HideError();

            string newPass = txtNewPassword.Password;
            string confirmPass = txtConfirmPassword.Password;

            if (string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
            {
                ShowGlobalError("Vui lòng nhập đầy đủ");

                txtNewPassword.BorderBrush = Brushes.Red;
                txtConfirmPassword.BorderBrush = Brushes.Red;
                return;
            }

            if (newPass != confirmPass)
            {

                _isSystemClearing = true;

                txtNewPassword.Clear();
                txtConfirmPassword.Clear();

                _isSystemClearing = false;

                ShowGlobalError("Mật khẩu không trùng nhau");

                txtNewPassword.BorderBrush = Brushes.Red;
                txtConfirmPassword.BorderBrush = Brushes.Red;

                txtNewPassword.Focus();
                return;
            }

      
            UpdatePasswordInDatabase(newPass);
        }

        private void ShowGlobalError(string message)
        {
            txbErrorMessage.Text = message;
            txbErrorMessage.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            if (txbErrorMessage.Visibility == Visibility.Visible)
            {
                txbErrorMessage.Visibility = Visibility.Collapsed;
                txtNewPassword.ClearValue(BorderBrushProperty);
                txtConfirmPassword.ClearValue(BorderBrushProperty);
            }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {

            if (_isSystemClearing) return;

            if (txbErrorMessage.Visibility == Visibility.Visible)
            {
                HideError();
            }
        }

        private void UpdatePasswordInDatabase(string newPass)
        {
            try
            {
                string sql = @"UPDATE TAIKHOAN
                               SET MatKhau = @pass
                               WHERE MANV = (SELECT MANV FROM NHANVIEN WHERE EMAIL = @email)";

                SqliteParameter[] p =
                {
                    new SqliteParameter("@pass", newPass),
                    new SqliteParameter("@email", _userEmail)
                };

                int rows = PharmaDistributionApp.Services.Database.ExecuteNonQuery(sql, p);

                if (rows > 0)
                {
                    MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                    var parentWindow = Window.GetWindow(this) as LoginWindow;
                    if (parentWindow != null)
                    {
                        parentWindow.NavigateToLogin();
                    }
                }
                else
                {
                    MessageBox.Show("Lỗi: Không tìm thấy tài khoản!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}