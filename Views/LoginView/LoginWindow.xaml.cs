using System.Windows;
using System.Windows.Input;

namespace PharmaDistributionApp.Views.LoginView
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Mặc định nạp màn hình Đăng nhập vào chỗ trống
            NavigateToLogin();
        }

        // 1. Chuyển sang màn hình Đăng nhập
        public void NavigateToLogin()
        {
            MainContent.Content = new LoginControl();
        }

        // 2. Chuyển sang màn hình Quên mật khẩu
        public void NavigateToForgotPass()
        {
            MainContent.Content = new ForgotPasswordWindow();
        }

        // 3. Chuyển sang màn hình Nhập mã xác nhận (QUAN TRỌNG: Đã sửa)
        // Hàm này phải nhận MÃ CODE thực tế từ ForgotPasswordWindow truyền sang
        public void NavigateToVerify(string code, string email)
        {
            // Truyền code thật và email vào màn hình VerifyCodeWindow
            MainContent.Content = new VerifyCodeWindow(code, email);
        }

        // 4. Chuyển sang màn hình Đặt lại mật khẩu
        public void NavigateToReset(string email)
        {
            MainContent.Content = new ResetPasswordWindow(email);
        }

        // --- CÁC HÀM XỬ LÝ SỰ KIỆN CỬA SỔ ---

        // Xử lý kéo thả cửa sổ (Vì WindowStyle=None)
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Xử lý thoát chung cho cả ứng dụng (Nút X ở góc trên bên phải)
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