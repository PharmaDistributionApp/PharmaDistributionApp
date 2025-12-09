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
            LoadRememberedUser();
        }

        private void LoadRememberedUser()
        {
            // Kiểm tra xem lần trước user có tick "Remember me" không
            if (Properties.Settings.Default.IsRemembered)
            {
                txtUsername.Text = Properties.Settings.Default.SavedUsername;
                chkRemember.IsChecked = true;

                // Mẹo: Tự động focus vào ô mật khẩu để nhập luôn cho lẹ
                txtPassword.Focus();
            }
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
                // --- SỬA 1: CẬP NHẬT CÂU SQL ĐỂ CHECK CẢ EMAIL ---
                // Thêm đoạn: OR N.EMAIL = @user
                string sql = @"
            SELECT T.*, N.EMAIL, N.TENNV 
            FROM TAIKHOAN T
            JOIN NHANVIEN N ON T.MANV = N.MANV
            WHERE (T.MANV = @user OR T.TENTK = @user OR N.EMAIL = @user) 
            AND T.MATKHAU = @pass";

                SQLiteParameter[] parameters = {
            new SQLiteParameter("@user", input),
            new SQLiteParameter("@pass", pass)
        };

                // Lưu ý: Đảm bảo class Database của bạn hỗ trợ trả về DataTable
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

                    // --- SỬA 2: LƯU TRẠNG THÁI "REMEMBER ME" ---
                    if (chkRemember.IsChecked == true)
                    {
                        Properties.Settings.Default.SavedUsername = input; // Lưu tên vừa nhập
                        Properties.Settings.Default.IsRemembered = true;   // Lưu trạng thái
                    }
                    else
                    {
                        Properties.Settings.Default.SavedUsername = "";    // Xóa tên
                        Properties.Settings.Default.IsRemembered = false;  // Xóa trạng thái
                    }
                    // Lệnh quan trọng để ghi xuống ổ cứng
                    Properties.Settings.Default.Save();

                    // --- LẤY THÔNG TIN & CHUYỂN MÀN HÌNH ---
                    string userEmail = row["EMAIL"].ToString();
                    string tenNhanVien = row["TENNV"].ToString();
                    string quyenHan = row["QUYENHAN"].ToString();

                    // Mở màn hình chính
                    MainWindow main = new MainWindow();
                    main.Show();

                    Window parentWindow = Window.GetWindow(this);
                    if (parentWindow != null) parentWindow.Close();
                }
                else
                {
                    ShowError("Sai ID, Tên tài khoản, Email hoặc Mật khẩu");
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