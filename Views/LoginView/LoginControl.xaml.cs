using System;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Services; // Để dùng class Employee và UserSession
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views.LoginView
{
    public partial class LoginControl : UserControl
    {
        // Định nghĩa màu viền mặc định và màu lỗi để khớp với Style trong XAML
        private readonly Brush _defaultBorder = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");
        private readonly Brush _errorBorder = Brushes.Red;

        public LoginControl()
        {
            InitializeComponent();
            LoadRememberedUser();
        }

        // Đổ dữ liệu từ Settings vào txtUsername và txtPassword nếu đã ghi nhớ
        private void LoadRememberedUser()
        {
            if (Properties.Settings.Default.IsRemembered)
            {
                txtUsername.Text = Properties.Settings.Default.SavedUsername;
                txtPassword.Password = Properties.Settings.Default.SavedPassword;
                chkRemember.IsChecked = true;
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            // 1. Reset giao diện về bình thường
            ResetUI();

            string input = txtUsername.Text.Trim();
            string pass = txtPassword.Password;
            bool hasValidationError = false;

            // Kiểm tra rỗng (Validation đầu vào khớp với các TextBox/PasswordBox trong XAML)
            if (string.IsNullOrEmpty(input))
            {
                txtUsername.BorderBrush = _errorBorder;
                hasValidationError = true;
            }

            if (string.IsNullOrEmpty(pass))
            {
                txtPassword.BorderBrush = _errorBorder;
                hasValidationError = true;
            }

            if (hasValidationError)
            {
                ShowError("Vui lòng điền đầy đủ thông tin");
                return;
            }

            try
            {
                // 2. Truy vấn lấy toàn bộ thông tin tài khoản và nhân viên (để lấy Chucvu, Avatar)
                string sql = @"
                    SELECT T.*, N.* FROM TAIKHOAN T
                    JOIN NHANVIEN N ON T.MANV = N.MANV
                    WHERE (T.MANV = @user OR N.EMAIL = @user OR N.TENNV = @user)";

                SQLiteParameter[] parameters = { new SQLiteParameter("@user", input) };
                DataTable dt = Database.GetTable(sql, parameters);

                if (dt.Rows.Count == 0)
                {
                    SetErrorState(txtUsername, "Tài khoản không tồn tại");
                    return;
                }

                DataRow row = dt.Rows[0];
                if (row["MATKHAU"].ToString() != pass)
                {
                    SetErrorState(txtPassword, "Mật khẩu không đúng");
                    return;
                }

                // Kiểm tra trạng thái khóa (TrangThai = 0 là bị khóa)
                if (Convert.ToInt64(row["TRANGTHAI"]) == 0)
                {
                    ShowError("Tài khoản đã bị khóa!");
                    return;
                }

                // --- ĐĂNG NHẬP THÀNH CÔNG ---

                // 3. Tạo đối tượng Employee và nạp dữ liệu (đặc biệt là Chucvu và AvatarBlob)
                Employee emp = new Employee
                {
                    Manv = row["MANV"].ToString(),
                    Tennv = row["TENNV"].ToString(),
                    Chucvu = row["CHUCVU"].ToString(),
                    Email = row["EMAIL"].ToString(),
                    AvatarBlob = row["AVATAR"] != DBNull.Value ? (byte[])row["AVATAR"] : null
                };

                // Gán emp (kiểu Employee) vào Session (bây giờ cũng là kiểu Employee)
                UserSession.CurrentUser = emp;
                UserSession.IsLoggedIn = true;

                MainWindow homeWin = new MainWindow(emp);
                homeWin.Show();

                // 6. Đóng cửa sổ hiện tại (Xử lý lỗi parentWindow trùng tên)
                Window currentWin = Window.GetWindow(this);
                if (currentWin != null) currentWin.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message);
            }
        }

        // --- CÁC HÀM UI HELPER KHỚP VỚI XAML ---

        private void SaveRememberMe(string username, string password)
        {
            if (chkRemember.IsChecked == true)
            {
                Properties.Settings.Default.SavedUsername = username;
                Properties.Settings.Default.SavedPassword = password;
                Properties.Settings.Default.IsRemembered = true;
            }
            else
            {
                Properties.Settings.Default.IsRemembered = false;
            }
            Properties.Settings.Default.Save();
        }

        private void ShowError(string message)
        {
            txbErrorMessage.Text = message;
            txbErrorMessage.Visibility = Visibility.Visible;
        }

        private void ResetUI()
        {
            txbErrorMessage.Visibility = Visibility.Collapsed;
            txtUsername.BorderBrush = _defaultBorder;
            txtPassword.BorderBrush = _defaultBorder;
        }

        private void SetErrorState(Control control, string message)
        {
            control.BorderBrush = _errorBorder;
            control.Focus();
            ShowError(message);
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var win = Window.GetWindow(this) as LoginWindow;
            if (win != null) win.NavigateToForgotPass();
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtUsername.BorderBrush == _errorBorder) ResetUI();
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtPassword.BorderBrush == _errorBorder) ResetUI();
        }
    }
}