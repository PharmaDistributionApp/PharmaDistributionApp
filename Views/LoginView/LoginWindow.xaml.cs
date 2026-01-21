using System.Windows;
using System.Windows.Input;

namespace PharmaDistributionApp.Views.LoginView
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            NavigateToLogin();
        }

        public void NavigateToLogin()
        {
            MainContent.Content = new LoginControl();
        }
        public void NavigateToForgotPass()
        {
            MainContent.Content = new ForgotPasswordWindow();
        }

        public void NavigateToVerify(string code, string email)
        {
            MainContent.Content = new VerifyCodeWindow(code, email);
        }

        public void NavigateToReset(string email)
        {
            MainContent.Content = new ResetPasswordWindow(email);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}