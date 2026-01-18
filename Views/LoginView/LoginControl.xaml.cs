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
                // 2. QUERY TÌM TÀI KHOẢN
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
                    SetErrorState(txtUsername, "Tài khoản không tồn tại");
                    return;
                }

                // --- TRƯỜNG HỢP 2: TÌM THẤY -> KIỂM TRA MẬT KHẨU ---
                DataRow row = dt.Rows[0];
                string dbPass = row["MATKHAU"].ToString();

                if (dbPass != pass)
                {
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

                // ============================================================
                // [QUAN TRỌNG]: LẤY ĐẦY ĐỦ THÔNG TIN USER (BAO GỒM CHỨC VỤ)
                // ============================================================

                string manv = row["MANV"].ToString();

                // Truy vấn lại bảng NHANVIEN để lấy đầy đủ thông tin
                string sqlUserFull = "SELECT * FROM NHANVIEN WHERE MANV = @id";

                DataTable dtUser = Database.GetTable(sqlUserFull, new SQLiteParameter[] {
                    new SQLiteParameter("@id", manv)
                });

                if (dtUser.Rows.Count > 0)
                {
                    DataRow userRow = dtUser.Rows[0];

                    // Tạo đối tượng Nhanvien và lưu vào Session
                    UserSession.CurrentUser = new Nhanvien
                    {
                        Manv = userRow["MANV"].ToString(),
                        // Xử lý tên cột HOTEN hoặc TENNV tùy DB
                        Tennv = userRow.Table.Columns.Contains("TENNV") ? userRow["TENNV"].ToString() : userRow["HOTEN"].ToString(),
                        Email = userRow["EMAIL"].ToString(),
                        Sdt = userRow["SDT"] != DBNull.Value ? userRow["SDT"].ToString() : "",
                        Diachi = userRow.Table.Columns.Contains("DIACHI") ? userRow["DIACHI"].ToString() : "",

                        // [ĐÂY LÀ DÒNG BẠN BỊ THIẾU TRƯỚC ĐÓ]
                        // Phải gán Chucvu thì màn hình Hóa đơn mới biết đây là Admin
                        Chucvu = userRow["CHUCVU"].ToString()
                    };

                    // Đánh dấu đã đăng nhập
                    UserSession.IsLoggedIn = true;
                }
                // ============================================================

                // Lưu ghi nhớ đăng nhập
                SaveRememberMe(input, pass);

                // Chuyển màn hình
                MainWindow main = new MainWindow();
                main.CurrentMaNV = manv; // Giữ lại dòng này để tương thích code cũ
                main.Show();

                // Đóng cửa sổ chứa LoginControl (thường là LoginWindow)
                Window parentWindow = Window.GetWindow(this);
                if (parentWindow != null) parentWindow.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đăng nhập: " + ex.Message);
            }
        }

        // --- CÁC HÀM HỖ TRỢ (GIỮ NGUYÊN) ---

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
            // Logic mở màn hình quên mật khẩu (Giữ nguyên logic của bạn)
            // Lưu ý: Cần ép kiểu về Window chứa Control này nếu method Navigate nằm ở đó
            var parentWindow = Window.GetWindow(this);
            // Ví dụ nếu parent là LoginWindow thì: ((LoginWindow)parentWindow).NavigateToForgotPass();
            // Ở đây tôi để code an toàn để tránh lỗi biên dịch nếu bạn chưa có method đó
            MessageBox.Show("Chức năng quên mật khẩu đang phát triển.", "Thông báo");
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