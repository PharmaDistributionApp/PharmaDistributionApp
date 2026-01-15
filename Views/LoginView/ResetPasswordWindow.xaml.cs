using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
// [SỬA ĐỔI 1]: Đổi thư viện SQLite sang SQL Server
using Microsoft.Data.SqlClient;
using PharmaDistributionApp.Services; // Đảm bảo đã using namespace chứa class Database

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

            // 1. KIỂM TRA RỖNG
            if (string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
            {
                ShowGlobalError("Vui lòng nhập đầy đủ");
                txtNewPassword.BorderBrush = Brushes.Red;
                txtConfirmPassword.BorderBrush = Brushes.Red;
                return;
            }

            // 2. KIỂM TRA KHỚP NHAU
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

            // 3. HỢP LỆ -> CẬP NHẬT DATABASE
            UpdatePasswordInDatabase(newPass);
        }

        // --- HÀM HỖ TRỢ GIAO DIỆN ---

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

        // --- DATABASE (ĐÃ SỬA CHO SQL SERVER) ---
        private void UpdatePasswordInDatabase(string newPass)
        {
            try
            {
                // Câu lệnh SQL giữ nguyên vì cú pháp Update giống nhau
                string sql = @"UPDATE TAIKHOAN
                               SET MATKHAU = @pass
                               WHERE MANV = (SELECT MANV FROM NHANVIEN WHERE EMAIL = @email)";

                // [SỬA ĐỔI 2]: Dùng SqlParameter thay vì SQLiteParameter
                SqlParameter[] p =
                {
                    new SqlParameter("@pass", newPass),
                    new SqlParameter("@email", _userEmail)
                };

                // Gọi hàm ExecuteNonQuery bên class Database (đã cập nhật hỗ trợ SqlParameter)
                int rows = Database.ExecuteNonQuery(sql, p);

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
                    MessageBox.Show("Lỗi: Không tìm thấy tài khoản hoặc Email không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}