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
        // Biến cờ để chặn sự kiện PasswordChanged khi hệ thống tự động xóa text
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
            // Reset trạng thái lỗi cũ trước khi kiểm tra mới
            HideError();

            string newPass = txtNewPassword.Password;
            string confirmPass = txtConfirmPassword.Password;

            // 1. KIỂM TRA RỖNG (Nếu 1 trong 2 ô chưa nhập)
            if (string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
            {
                ShowGlobalError("Vui lòng nhập đầy đủ");

                // Tô đỏ cả 2 ô theo yêu cầu
                txtNewPassword.BorderBrush = Brushes.Red;
                txtConfirmPassword.BorderBrush = Brushes.Red;
                return;
            }

            // 2. KIỂM TRA KHỚP NHAU
            if (newPass != confirmPass)
            {
                // Bật cờ lên để sự kiện PasswordChanged không tự xóa viền đỏ
                _isSystemClearing = true;

                // Xóa nội dung 2 ô theo yêu cầu
                txtNewPassword.Clear();
                txtConfirmPassword.Clear();

                // Tắt cờ sau khi xóa xong
                _isSystemClearing = false;

                ShowGlobalError("Mật khẩu không trùng nhau");

                // Tô đỏ lại sau khi xóa (vì Clear() có thể đã reset giao diện)
                txtNewPassword.BorderBrush = Brushes.Red;
                txtConfirmPassword.BorderBrush = Brushes.Red;

                // Focus lại vào ô đầu tiên
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

                // Trả lại quyền điều khiển màu viền cho XAML (Xanh/Xám)
                txtNewPassword.ClearValue(BorderBrushProperty);
                txtConfirmPassword.ClearValue(BorderBrushProperty);
            }
        }

        // Sự kiện dùng chung cho cả 2 ô PasswordBox
        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            // Nếu hệ thống đang tự xóa (do nhập sai) thì KHÔNG được reset lỗi
            if (_isSystemClearing) return;

            // Nếu người dùng đang tự nhập và đang có lỗi hiển thị -> Tắt lỗi đi
            if (txbErrorMessage.Visibility == Visibility.Visible)
            {
                HideError();
            }
        }

        // --- DATABASE ---
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