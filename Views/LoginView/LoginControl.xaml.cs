using System;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // Cần thêm thư viện này để dùng màu sắc (Brushes)
using PharmaDistributionApp.Services;

namespace PharmaDistributionApp.Views.LoginView
{
    public partial class LoginControl : UserControl
    {
        // Định nghĩa màu viền mặc định và màu lỗi
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
            // 1. Reset giao diện về bình thường (Xóa lỗi cũ)
            ResetUI();

            string input = txtUsername.Text.Trim();
            string pass = txtPassword.Password;
            bool hasValidationError = false;

            // Kiểm tra rỗng (Validation đầu vào)
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
                // 2. QUERY CHỈ TÌM TÀI KHOẢN (Bỏ phần check mật khẩu ở đây)
                string sql = @"
                    SELECT T.*, N.EMAIL, N.TENNV 
                    FROM TAIKHOAN T
                    JOIN NHANVIEN N ON T.MANV = N.MANV
                    WHERE (
                            T.MANV = @user      
                         OR N.EMAIL = @user     
                         OR N.TENNV = @user     
                          )";

                SQLiteParameter[] parameters = {
                    new SQLiteParameter("@user", input)
                };

                DataTable dt = Database.GetTable(sql, parameters);

                // --- TRƯỜNG HỢP 1: KHÔNG TÌM THẤY TÀI KHOẢN ---
                if (dt.Rows.Count == 0)
                {
                    // Tô đỏ ô Username
                    SetErrorState(txtUsername, "Tài khoản không tồn tại");
                    return;
                }

                // --- TRƯỜNG HỢP 2: TÌM THẤY -> KIỂM TRA MẬT KHẨU ---
                DataRow row = dt.Rows[0];
                string dbPass = row["MATKHAU"].ToString(); // Lấy mật khẩu trong DB

                // So sánh mật khẩu (Phân biệt hoa thường)
                if (dbPass != pass)
                {
                    // Tô đỏ ô Password
                    SetErrorState(txtPassword, "Mật khẩu không đúng");
                    return;
                }

                // --- TRƯỜNG HỢP 3: KIỂM TRA TRẠNG THÁI KHÓA ---
                long trangThai = Convert.ToInt64(row["TRANGTHAI"]);
                if (trangThai == 0)
                {
                    ShowError("Tài khoản đã bị khóa!");
                    return;
                }

                // --- ĐĂNG NHẬP THÀNH CÔNG ---

                // Gọi hàm lưu với tên biến mới
                SaveRememberMe(input, pass);

                // Chuyển màn hình
                MainWindow main = new MainWindow();
                main.CurrentMaNV = row["MANV"]?.ToString(); // Lưu MANV vào biến tĩnh
                main.Show();

                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null) parentWindow.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }
        private void SaveRememberMe(string username, string password)
        {
            if (chkRemember.IsChecked == true)
            {
                Properties.Settings.Default.SavedUsername = username;
                Properties.Settings.Default.SavedPassword = password;

                // Đã đổi thành IsRemembered
                Properties.Settings.Default.IsRemembered = true;
            }
            else
            {
                Properties.Settings.Default.SavedUsername = "";
                Properties.Settings.Default.SavedPassword = "";

                // Đã đổi thành IsRemembered
                Properties.Settings.Default.IsRemembered = false;
            }
            Properties.Settings.Default.Save();
        }


        // Hàm hiển thị lỗi chung chung (không tô viền)
        private void ShowError(string message)
        {
            txbErrorMessage.Text = message;
            txbErrorMessage.Visibility = Visibility.Visible;
        }

        // --- CÁC HÀM XỬ LÝ GIAO DIỆN (UI) MỚI ---

        // Hàm Reset màu viền về mặc định (Xám)
        private void ResetUI()
        {
            txbErrorMessage.Visibility = Visibility.Collapsed;
            if (txtUsername.BorderBrush == _errorBorder) txtUsername.BorderBrush = _defaultBorder;
            if (txtPassword.BorderBrush == _errorBorder) txtPassword.BorderBrush = _defaultBorder;
        }

        // Hàm báo lỗi: Tô đỏ viền control + Hiện thông báo + Focus con trỏ
        private void SetErrorState(Control control, string message)
        {
            // 1. Đổi màu viền thành Đỏ
            control.BorderBrush = _errorBorder;

            // 2. Focus vào ô bị sai
            control.Focus();

            // 3. Hiện thông báo lỗi
            ShowError(message);
        }
        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var parentWindow = Window.GetWindow(this) as LoginWindow;
            if (parentWindow != null)
            {
                parentWindow.NavigateToForgotPass();
            }
        }
        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtUsername.BorderBrush == _errorBorder)
            {
                txtUsername.BorderBrush = _defaultBorder;
                txbErrorMessage.Visibility = Visibility.Collapsed;
            }
        }
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtPassword.BorderBrush == _errorBorder)
            {
                txtPassword.BorderBrush = _defaultBorder;
                txbErrorMessage.Visibility = Visibility.Collapsed;
            }

        }


    }
}
