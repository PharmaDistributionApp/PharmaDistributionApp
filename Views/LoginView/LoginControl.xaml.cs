using System;
using System.Data;
using Microsoft.Data.SqlClient; // Dùng SQL Server
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Services;
using PharmaDistributionApp.Models; // Chứa UserSession

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

            if (string.IsNullOrEmpty(input)) { SetErrorState(txtUsername, "Vui lòng nhập thông tin"); return; }
            if (string.IsNullOrEmpty(pass)) { SetErrorState(txtPassword, "Vui lòng nhập mật khẩu"); return; }

            try
            {
                // [CẬP NHẬT SQL]: Tìm theo Mã NV, Email HOẶC Tên nhân viên
                string sql = @"
            SELECT T.MANV, T.MATKHAU, T.TRANGTHAI
            FROM TAIKHOAN T
            JOIN NHANVIEN N ON T.MANV = N.MANV
            WHERE T.MANV = @user 
               OR N.EMAIL = @user 
               OR N.TENNV = @user"; // Thêm dòng này để đăng nhập bằng tên

                // Lưu ý: SQL Server tự động xử lý chữ hoa/thường và tiếng Việt có dấu (nếu Collation chuẩn)
                SqlParameter[] parameters = { new SqlParameter("@user", input) };
                DataTable dt = Database.GetTable(sql, parameters);

                if (dt == null || dt.Rows.Count == 0)
                {
                    SetErrorState(txtUsername, "Tài khoản không tồn tại");
                    return;
                }

                // Nếu tìm thấy nhiều người trùng tên (hiếm gặp nhưng có thể), lấy người đầu tiên
                DataRow row = dt.Rows[0];
                string dbPass = row["MATKHAU"].ToString();

                if (dbPass != pass)
                {
                    SetErrorState(txtPassword, "Mật khẩu không đúng");
                    return;
                }

                long trangThai = row["TRANGTHAI"] != DBNull.Value ? Convert.ToInt64(row["TRANGTHAI"]) : 1;
                if (trangThai == 0)
                {
                    ShowError("Tài khoản đã bị khóa!");
                    return;
                }

                // Lưu phiên và chuyển màn hình
                UserSession.CurrentMaNV = row["MANV"].ToString();
                SaveRememberMe(input, pass);

                MainWindow main = new MainWindow();
                main.Show();

                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null) parentWindow.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đăng nhập: " + ex.Message);
            }
        }

        // --- CÁC HÀM HỖ TRỢ ---
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
                Properties.Settings.Default.SavedUsername = "";
                Properties.Settings.Default.SavedPassword = "";
                Properties.Settings.Default.IsRemembered = false;
            }
            Properties.Settings.Default.Save();
        }

        private void ShowError(string message) { txbErrorMessage.Text = message; txbErrorMessage.Visibility = Visibility.Visible; }
        private void ResetUI() { txbErrorMessage.Visibility = Visibility.Collapsed; txtUsername.BorderBrush = _defaultBorder; txtPassword.BorderBrush = _defaultBorder; }
        private void SetErrorState(Control control, string message) { control.BorderBrush = _errorBorder; control.Focus(); ShowError(message); }
        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { var parent = Window.GetWindow(this) as LoginWindow; parent?.NavigateToForgotPass(); }
        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e) { if (txtUsername.BorderBrush == _errorBorder) ResetUI(); }
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e) { if (txtPassword.BorderBrush == _errorBorder) ResetUI(); }
    }
}