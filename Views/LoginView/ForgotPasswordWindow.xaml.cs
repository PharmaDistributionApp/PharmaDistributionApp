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

    public partial class ForgotPasswordWindow : UserControl 
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        private void btnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            var parent = Window.GetWindow(this) as LoginWindow;
            if (parent != null)
            {
                parent.NavigateToLogin(); 
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string email = txtRecoveryEmail.Text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                HienThiLoi("Vui lòng nhập email của bạn");
                return;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                HienThiLoi("Email không hợp lệ");
                return;
            }

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

                EmailService emailService = new EmailService();
                string otpCode = emailService.GenerateOTP();

                emailService.SendVerificationCode(email, otpCode);

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
                txtRecoveryEmail.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");
            }
        }

        private void HienThiLoi(string noiDung)
        {
            txbErrorContent.Text = noiDung;
            txtRecoveryEmail.BorderBrush = Brushes.Red;
            pnlErrorMessage.Visibility = Visibility.Visible;
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}