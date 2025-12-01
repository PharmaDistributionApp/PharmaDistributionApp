using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PharmaDistributionApp.Views
{
    /// <summary>
    /// Interaction logic for ForgotPasswordWindow.xaml
    /// </summary>
    public partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        private void btnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
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
                EmailService emailService = new EmailService();

                string otpCode = emailService.GenerateOTP();

                emailService.SendVerificationCode(email, otpCode);

                VerifyCodeWindow verifyWindow = new VerifyCodeWindow(otpCode, email);

                verifyWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gửi thất bại: " + ex.Message);
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
            txtRecoveryEmail.BorderBrush = Brushes.Red;

            pnlErrorMessage.Visibility = Visibility.Visible;
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
