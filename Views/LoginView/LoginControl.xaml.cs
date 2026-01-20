using System;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Views;
using PharmaDistributionApp.Services;
using Microsoft.Data.Sqlite;

namespace PharmaDistributionApp.Views.LoginView
{
    public partial class LoginControl : UserControl
    {
        private readonly Brush _defaultBorder = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");
        private readonly Brush _errorBorder = Brushes.Red;

        public LoginControl()
        {
            InitializeComponent();
            LoadRememberedUser();
        }

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
            ResetUI();
            string input = txtUsername.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(input)) { SetErrorState(txtUsername, "Vui lòng nhập tài khoản"); return; }
            if (string.IsNullOrEmpty(pass)) { SetErrorState(txtPassword, "Vui lòng nhập mật khẩu"); return; }

            try
            {
                // Truy vấn lấy cả thông tin tài khoản và nhân viên
                string sql = @"
                    SELECT T.MANV, T.MATKHAU, T.TRANGTHAI, 
                           N.TENNV, N.CHUCVU, N.EMAIL, N.AVATAR 
                    FROM TAIKHOAN T 
                    LEFT JOIN NHANVIEN N ON T.MANV = N.MANV 
                    WHERE (T.MANV = @user OR N.EMAIL = @user)";

                var parameters = new SqliteParameter[]
                 {
                    new SqliteParameter("@user", input)
                 };
                DataTable dt = Database.GetTable(sql, parameters);

                if (dt.Rows.Count == 0) { SetErrorState(txtUsername, "Tài khoản không tồn tại"); return; }

                DataRow row = dt.Rows[0];
                if (row["MATKHAU"].ToString() != pass) { SetErrorState(txtPassword, "Mật khẩu không đúng"); return; }
                if (Convert.ToInt32(row["TRANGTHAI"]) == 0) { ShowError("Tài khoản đã bị khóa!"); return; }

                // --- ĐĂNG NHẬP THÀNH CÔNG ---

                // 1. Lưu session (Sẽ hết lỗi vì đã có using ở trên)
                UserSession.CurrentUser = new Employee
                {
                    Manv = row["MANV"].ToString(),
                    Tennv = row["TENNV"] != DBNull.Value ? row["TENNV"].ToString() : "Unknown",
                    Chucvu = row["CHUCVU"] != DBNull.Value ? row["CHUCVU"].ToString() : "NhanVien",
                    Email = row["EMAIL"] != DBNull.Value ? row["EMAIL"].ToString() : "",
                    AvatarBlob = row["AVATAR"] != DBNull.Value ? (byte[])row["AVATAR"] : null
                };
                UserSession.IsLoggedIn = true;

                // 2. Lưu ghi nhớ
                SaveRememberMe(input, pass);

                // 3. Chuyển màn hình
                MainWindow main = new MainWindow();
                main.Show();

                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi đăng nhập: " + ex.Message); }
        }

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
                Properties.Settings.Default.SavedUsername = "";
                Properties.Settings.Default.SavedPassword = "";
            }
            Properties.Settings.Default.Save();
        }

        // Các hàm giao diện phụ trợ
        private void ShowError(string msg) { txbErrorMessage.Text = msg; txbErrorMessage.Visibility = Visibility.Visible; }
        private void ResetUI() { txbErrorMessage.Visibility = Visibility.Collapsed; txtUsername.BorderBrush = _defaultBorder; txtPassword.BorderBrush = _defaultBorder; }
        private void SetErrorState(Control c, string msg) { c.BorderBrush = _errorBorder; c.Focus(); ShowError(msg); }
        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { /* Code Quên mật khẩu */ }
        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e) { if (txtUsername.BorderBrush == _errorBorder) ResetUI(); }
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e) { if (txtPassword.BorderBrush == _errorBorder) ResetUI(); }
    }
}