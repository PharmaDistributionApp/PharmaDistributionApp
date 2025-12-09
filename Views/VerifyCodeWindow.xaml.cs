using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PharmaDistributionApp.Views
{
    public partial class VerifyCodeWindow : UserControl
    {
        private string _systemCode;
        private string _userEmail;

        public VerifyCodeWindow(string code, string email)
        {
            InitializeComponent();
            _systemCode = code;
            _userEmail = email;

            // Sự kiện Loaded để đảm bảo UI đã vẽ xong mới Focus
            this.Loaded += (s, e) => txtC1.Focus();
        }

        public VerifyCodeWindow()
        {
            InitializeComponent();
        }

        // 1. TỰ ĐỘNG NHẢY Ô KHI NHẬP SỐ
        private void TxtCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox currentBox = sender as TextBox;

            // Nếu nhập 1 ký tự thì nhảy sang ô sau
            if (currentBox.Text.Length == 1)
            {
                var request = new TraversalRequest(FocusNavigationDirection.Next);
                currentBox.MoveFocus(request);
            }
        }

        // 2. XỬ LÝ NÚT BACKSPACE (XÓA LÙI) & CHẶN NHẬP CHỮ
        // Bạn cần gán sự kiện này cho cả 6 ô TextBox trong file XAML: PreviewKeyDown="TxtCode_PreviewKeyDown"
        private void TxtCode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox currentBox = sender as TextBox;

            // Xử lý nút Xóa (Backspace)
            if (e.Key == Key.Back)
            {
                // Nếu ô hiện tại rỗng, lùi về ô trước đó
                if (string.IsNullOrEmpty(currentBox.Text))
                {
                    var request = new TraversalRequest(FocusNavigationDirection.Previous);
                    currentBox.MoveFocus(request);
                }
            }
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string inputCode = txtC1.Text + txtC2.Text + txtC3.Text + txtC4.Text + txtC5.Text + txtC6.Text;

            // SỬA: Bỏ điều kiện string.IsNullOrEmpty để bảo mật hơn
            if (inputCode == _systemCode)
            {
                var parentWindow = Window.GetWindow(this) as LoginWindow;
                if (parentWindow != null)
                {
                    parentWindow.NavigateToReset(_userEmail);
                }
            }
            else
            {
                MessageBox.Show("Mã xác minh không chính xác!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                // Xóa đi để nhập lại
                ClearInputs();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this) as LoginWindow;
            if (parentWindow != null)
            {
                parentWindow.NavigateToForgotPass();
            }
        }

        private void ClearInputs()
        {
            txtC1.Clear(); txtC2.Clear(); txtC3.Clear();
            txtC4.Clear(); txtC5.Clear(); txtC6.Clear();
            txtC1.Focus();
        }
    }
}