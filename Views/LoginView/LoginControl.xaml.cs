using System;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Services;
using PharmaDistributionApp.Models;

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
            bool hasValidationError = false;

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
                // 1. Truy vấn lấy thông tin Tài khoản và Nhân viên cơ bản
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
                string dbPass = row["MATKHAU"].ToString();

                if (dbPass != pass)
                {
                    SetErrorState(txtPassword, "Mật khẩu không đúng");
                    return;
                }

                long trangThai = Convert.ToInt64(row["TRANGTHAI"]);
                if (trangThai == 0)
                {
                    ShowError("Tài khoản đã bị khóa!");
                    return;
                }

                // 2. Tạo đối tượng Nhanvien/Employee đầy đủ
                // Chúng ta ưu tiên dùng class Nhanvien (Model) vì nó khớp với UserSession của bạn
                Nhanvien currentUser = new Nhanvien
                {
                    Manv = row["MANV"].ToString(),
                    // Kiểm tra linh hoạt giữa cột TENNV và HOTEN
                    Tennv = dt.Columns.Contains("TENNV") ? row["TENNV"].ToString() : row["HOTEN"].ToString(),
                    Chucvu = row["CHUCVU"].ToString(),
                    Email = row["EMAIL"].ToString(),
                    Sdt = row["SDT"] != DBNull.Value ? row["SDT"].ToString() : "",
                    Diachi = row["DIACHI"] != DBNull.Value ? row["DIACHI"].ToString() : "",
                    Cccd = row["CCCD"] != DBNull.Value ? row["CCCD"].ToString() : "",
                    Gioitinh = row["GIOITINH"] != DBNull.Value ? row["GIOITINH"].ToString() : ""
                };

                // Xử lý Ngày sinh (Bây giờ Model là DateTime?)
                if (dt.Columns.Contains("NGAYSINH") && row["NGAYSINH"] != DBNull.Value)
                {
                    if (DateTime.TryParse(row["NGAYSINH"].ToString(), out DateTime dateVal))
                    {
                        // Gán trực tiếp vì cả hai đều là DateTime
                        currentUser.Ngaysinh = dateVal;
                    }
                }
                // Xử lý Avatar (byte[])
                if (dt.Columns.Contains("AVATAR") && row["AVATAR"] != DBNull.Value)
                {
                    currentUser.Avatar = (byte[])row["AVATAR"];
                }

                // 3. Lưu vào Session
                UserSession.CurrentUser = currentUser;
                UserSession.IsLoggedIn = true;

                // 4. Lưu ghi nhớ đăng nhập
                SaveRememberMe(input, pass);

                // 5. Mở MainWindow (Tương thích với cả 2 cách truyền dữ liệu)
                MainWindow main = new MainWindow();
                // Nếu MainWindow của bạn dùng CurrentMaNV để load data:
                try { main.CurrentMaNV = currentUser.Manv; } catch { }

                main.Show();

                // Đóng cửa sổ đăng nhập
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null) parentWindow.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống khi đăng nhập: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveRememberMe(string username, string password)
        {
            if (chkRemember.IsChecked == true)
            {
                // Gán trực tiếp giá trị vào các key đã tạo trong Project Settings
                Properties.Settings.Default.SavedUsername = username;
                Properties.Settings.Default.SavedPassword = password;
                Properties.Settings.Default.IsRemembered = true;
            }
            else
            {
                // Xóa thông tin nếu người dùng không chọn "Ghi nhớ"
                Properties.Settings.Default.SavedUsername = "";
                Properties.Settings.Default.SavedPassword = "";
                Properties.Settings.Default.IsRemembered = false;
            }

            // Lưu lại thay đổi xuống file cấu hình của máy
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
            var parentWindow = Window.GetWindow(this) as LoginWindow;
            if (parentWindow != null)
            {
                try { parentWindow.NavigateToForgotPass(); }
                catch { MessageBox.Show("Chức năng quên mật khẩu đang được bảo trì."); }
            }
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