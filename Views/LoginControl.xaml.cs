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
                // --- LOGIC ĐĂNG NHẬP THÔNG MINH ---
                // Cho phép nhập: Mã NV (NV001) HOẶC Email HOẶC Tên thật (Huỳnh Long Bảo Khanh)
                string sql = @"
            SELECT T.*, N.EMAIL, N.TENNV 
            FROM TAIKHOAN T
            JOIN NHANVIEN N ON T.MANV = N.MANV
            WHERE (
                    T.MANV = @user      -- Cách 1: Nhập Mã NV
                 OR N.EMAIL = @user     -- Cách 2: Nhập Email
                 OR N.TENNV = @user     -- Cách 3: Nhập Họ tên thật
                  ) 
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

                    // --- LƯU REMEMBER ME ---
                    if (chkRemember.IsChecked == true)
                    {
                        Properties.Settings.Default.SavedUsername = input;
                        Properties.Settings.Default.IsRemembered = true;
                    }
                    else
                    {
                        Properties.Settings.Default.SavedUsername = "";
                        Properties.Settings.Default.IsRemembered = false;
                    }
                    Properties.Settings.Default.Save();

                    // Lấy thông tin hiển thị
                    string tenNhanVien = row["TENNV"].ToString();

                    // Mở Main Window
                    MainWindow main = new MainWindow();
                    main.Show();

                    Window parentWindow = Window.GetWindow(this);
                    if (parentWindow != null) parentWindow.Close();
                }
                else
                {
                    ShowError("Sai thông tin đăng nhập hoặc mật khẩu");
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