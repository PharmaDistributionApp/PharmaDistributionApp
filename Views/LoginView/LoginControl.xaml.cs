using System;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Services; // Để dùng class Employee

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
            // 1. Reset giao diện
            ResetUI();

            string input = txtUsername.Text.Trim();
            string pass = txtPassword.Password;
            bool hasValidationError = false;

            // Validation đầu vào
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
                // 2. QUERY: Lấy TẤT CẢ thông tin nhân viên (N.*) để có Avatar, Chức vụ...
                string sql = @"
                    SELECT T.*, N.* FROM TAIKHOAN T
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
                    SetErrorState(txtUsername, "Tài khoản không tồn tại");
                    return;
                }

                // --- TRƯỜNG HỢP 2: CHECK MẬT KHẨU ---
                DataRow row = dt.Rows[0];
                string dbPass = row["MATKHAU"].ToString();

                if (dbPass != pass)
                {
                    SetErrorState(txtPassword, "Mật khẩu không đúng");
                    return;
                }

                // --- TRƯỜNG HỢP 3: CHECK KHÓA ---
                long trangThai = Convert.ToInt64(row["TRANGTHAI"]); // Cột trạng thái của bảng NHANVIEN hoặc TAIKHOAN
                if (trangThai == 0)
                {
                    ShowError("Tài khoản đã bị khóa!");
                    return;
                }

                // --- ĐĂNG NHẬP THÀNH CÔNG ---

                // 3. Tạo đối tượng Employee đầy đủ để truyền sang MainWindow
                Employee currentUser = new Employee();
                currentUser.Manv = row["MANV"].ToString();
                currentUser.Tennv = row["TENNV"].ToString();
                currentUser.Chucvu = row["CHUCVU"].ToString();
                currentUser.Email = row["EMAIL"].ToString();

                // Xử lý các trường có thể Null
                currentUser.Sdt = row["SDT"] != DBNull.Value ? row["SDT"].ToString() : "";
                currentUser.Diachi = row["DIACHI"] != DBNull.Value ? row["DIACHI"].ToString() : "";
                currentUser.Cccd = row["CCCD"] != DBNull.Value ? row["CCCD"].ToString() : "";
                currentUser.GioiTinh = row["GIOITINH"] != DBNull.Value ? row["GIOITINH"].ToString() : "";
                currentUser.TrangThai = Convert.ToInt32(trangThai);

                if (row["NGAYSINH"] != DBNull.Value && DateTime.TryParse(row["NGAYSINH"].ToString(), out DateTime dateVal))
                {
                    currentUser.Ngaysinh = dateVal;
                }

                // Quan trọng: Lấy Avatar
                if (row["AVATAR"] != DBNull.Value)
                {
                    currentUser.AvatarBlob = (byte[])row["AVATAR"];
                }

                // 4. Lưu ghi nhớ đăng nhập
                SaveRememberMe(input, pass);

                // 5. Mở MainWindow và truyền User vào
                // (Lưu ý: MainWindow phải có constructor nhận Employee như bài trước)
                MainWindow main = new MainWindow(currentUser);
                main.Show();

                // Đóng LoginWindow
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null) parentWindow.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đăng nhập: " + ex.Message);
            }
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
                Properties.Settings.Default.SavedUsername = "";
                Properties.Settings.Default.SavedPassword = "";
                Properties.Settings.Default.IsRemembered = false;
            }
            Properties.Settings.Default.Save();
        }

        // --- CÁC HÀM UI HELPER ---
        private void ShowError(string message)
        {
            txbErrorMessage.Text = message;
            txbErrorMessage.Visibility = Visibility.Visible;
        }

        private void ResetUI()
        {
            txbErrorMessage.Visibility = Visibility.Collapsed;
            if (txtUsername.BorderBrush == _errorBorder) txtUsername.BorderBrush = _defaultBorder;
            if (txtPassword.BorderBrush == _errorBorder) txtPassword.BorderBrush = _defaultBorder;
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