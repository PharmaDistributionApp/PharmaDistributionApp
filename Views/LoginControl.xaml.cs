using System;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PharmaDistributionApp.Services;

namespace PharmaDistributionApp.Views
{
    public partial class LoginControl : UserControl
    {
        public LoginControl()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            txbErrorMessage.Visibility = Visibility.Collapsed;

            string input = txtUsername.Text.Trim();
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pass))
            {
                ShowError("Vui lòng nhập đầy đủ thông tin");
                return;
            }

            try
            {
                // --- CÂU LỆNH SQL JOIN ĐỂ LẤY THÊM EMAIL ---
                // T = Bảng TAIKHOAN, N = Bảng NHANVIEN
                // Lấy tất cả cột của Tài khoản (T.*) và lấy thêm cột Email của Nhân viên (N.EMAIL)
                string sql = @"
                    SELECT T.*, N.EMAIL, N.TENNV 
                    FROM TAIKHOAN T
                    JOIN NHANVIEN N ON T.MANV = N.MANV
                    WHERE (T.MANV = @user OR T.TENTK = @user) 
                    AND T.MATKHAU = @pass";

                SQLiteParameter[] parameters = {
                    new SQLiteParameter("@user", input),
                    new SQLiteParameter("@pass", pass)
                };

                DataTable dt = Database.GetTable(sql, parameters);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    long trangThai = Convert.ToInt64(row["TRANGTHAI"]);

                    if (trangThai == 0)
                    {
                        ShowError("Tài khoản đã bị khóa!");
                        return;
                    }

                    // --- LẤY THÔNG TIN NGƯỜI DÙNG ---
                    string userEmail = row["EMAIL"].ToString(); // Đã lấy được Email!
                    string tenNhanVien = row["TENNV"].ToString(); // Lấy được cả tên nhân viên
                    string quyenHan = row["QUYENHAN"].ToString();

                    // --- ĐĂNG NHẬP THÀNH CÔNG ---

                    // Bạn có thể truyền thông tin này sang MainWindow
                    // Ví dụ: new MainWindow(tenNhanVien, userEmail)
                    MainWindow main = new MainWindow();
                    main.Show();

                    Window parentWindow = Window.GetWindow(this);
                    if (parentWindow != null) parentWindow.Close();
                }
                else
                {
                    ShowError("Sai ID hoặc mật khẩu");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var parentWindow = Window.GetWindow(this) as LoginWindow;
            if (parentWindow != null)
            {
                parentWindow.NavigateToForgotPass();
            }
        }

        private void ShowError(string message)
        {
            txbErrorMessage.Text = message;
            txbErrorMessage.Visibility = Visibility.Visible;
        }
    }
}