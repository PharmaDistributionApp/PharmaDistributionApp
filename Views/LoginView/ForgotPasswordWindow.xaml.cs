using MaterialDesignThemes.Wpf;
using PharmaDistributionApp.Services;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using PharmaDistributionApp.Views.LoginView;

namespace PharmaDistributionApp.Views
{
    /// <summary>
    /// Interaction logic for ForgotPasswordWindow.xaml
    /// </summary>
    public partial class ForgotPasswordWindow : UserControl // 1. Đổi từ Window sang UserControl
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        // XỬ LÝ QUAY LẠI ĐĂNG NHẬP
        private void btnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            var parent = Window.GetWindow(this) as LoginWindow;
            if (parent != null)
            {
                parent.NavigateToLogin(); // Gọi hàm điều hướng của cha
            }
        }

        // XỬ LÝ GỬI MÃ
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string email = txtRecoveryEmail.Text.Trim();

            // 1. Kiểm tra rỗng
            if (string.IsNullOrEmpty(email))
            {
                HienThiLoi("Vui lòng nhập email của bạn");
                return;
            }

            // 2. Kiểm tra định dạng Email cơ bản
            if (!email.Contains("@") || !email.Contains("."))
            {
                HienThiLoi("Email không hợp lệ");
                return;
            }

            // Hiển thị con trỏ xoay (Loading)
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                string sqlCheck = "SELECT COUNT(*) FROM NHANVIEN WHERE EMAIL = @email";
                SqliteParameter[] p =
                {
                    new SqliteParameter("@email", email)
                };

                object result = Database.ExecuteScalar(sqlCheck, p);
                long count = 0;

                if (result != null && result != DBNull.Value)
                {
                    count = Convert.ToInt64(result);
                }

                if (count == 0)
                {
                    Mouse.OverrideCursor = null;
                    HienThiLoi("Email này chưa được đăng ký trong hệ thống!");
                    return;
                }

                // 3. NẾU CÓ EMAIL -> TIẾN HÀNH GỬI MÃ
                EmailService emailService = new EmailService();
                string otpCode = emailService.GenerateOTP();

                emailService.SendVerificationCode(email, otpCode);

                // 4. Chuyển màn hình
                var parent = Window.GetWindow(this) as LoginWindow;
                if (parent != null)
                {
                    parent.NavigateToVerify(otpCode, email);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void txtRecoveryEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (pnlErrorMessage.Visibility == Visibility.Visible)
            {
                pnlErrorMessage.Visibility = Visibility.Collapsed;
                // Trả lại màu viền xám mặc định
                txtRecoveryEmail.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");
            }
        }

        // HÀM HIỂN THỊ LỖI
        private void HienThiLoi(string noiDung)
        {
            txbErrorContent.Text = noiDung;

            // 2. Viền đỏ ô nhập
            txtRecoveryEmail.BorderBrush = Brushes.Red;

            // 3. Hiện thông báo
            pnlErrorMessage.Visibility = Visibility.Visible;
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}