using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PharmaDistributionApp.Views.LoginView
{
    public partial class VerifyCodeWindow : UserControl
    {
        private string _systemCode;
        private string _userEmail;
        private TextBox[] _boxes;

        public VerifyCodeWindow(string code, string email) : this()
        {
            _systemCode = code;
            _userEmail = email;
        }

        public VerifyCodeWindow()
        {
            InitializeComponent();
            InitializeOtpBoxes();
        }

        private void InitializeOtpBoxes()
        {
            _boxes = new TextBox[] { txtC1, txtC2, txtC3, txtC4, txtC5, txtC6 };

            foreach (var box in _boxes)
            {
                // Gán các sự kiện
                box.TextChanged += TxtCode_TextChanged;
                box.PreviewKeyDown += TxtCode_PreviewKeyDown;
                box.PreviewTextInput += TxtCode_PreviewTextInput;

                // Xử lý giao diện khi click chuột
                box.GotFocus += (s, e) => {
                    (s as TextBox).SelectAll();
                    ClearError(); // [QUAN TRỌNG] Click vào là xóa báo lỗi ngay
                };

                box.PreviewMouseDown += (s, e) =>
                {
                    var textBox = s as TextBox;
                    if (!textBox.IsKeyboardFocusWithin)
                    {
                        textBox.Focus();
                        e.Handled = true;
                    }
                };
            }
            this.Loaded += (s, e) => txtC1.Focus();
        }

        // --- HÀM UI: HIỂN THỊ & XÓA LỖI (GIỐNG GOOGLE) ---

        private void ShowError(string message)
        {
            // 1. Hiện dòng chữ đỏ
            txbError.Text = message;
            txbError.Visibility = Visibility.Visible;

            // 2. Tô viền đỏ tất cả các ô
            foreach (var box in _boxes)
            {
                box.BorderBrush = Brushes.Red;
            }

            // 3. Rung nhẹ (Tuỳ chọn - nếu muốn làm sau)
        }

        private void ClearError()
        {
            // Chỉ chạy nếu đang hiện lỗi
            if (txbError.Visibility == Visibility.Visible)
            {
                txbError.Visibility = Visibility.Collapsed;

                // Trả lại quyền kiểm soát màu cho Style (Xám/Xanh)
                foreach (var box in _boxes)
                {
                    box.ClearValue(BorderBrushProperty);
                }
            }
        }

        // --- LOGIC SỰ KIỆN ---

        private void TxtCode_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            ClearError(); // Người dùng nhập lại -> Xóa lỗi
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TxtCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox currentBox = sender as TextBox;

            if (currentBox.Text.Length == 1)
            {
                int index = Array.IndexOf(_boxes, currentBox);
                if (index < 5)
                {
                    Dispatcher.InvokeAsync(() => _boxes[index + 1].Focus(), DispatcherPriority.Input);
                }
                else
                {
                    btnConfirm.Focus();
                }
            }
        }

        private void TxtCode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ClearError(); // Người dùng nhấn phím (kể cả xóa) -> Xóa lỗi

            TextBox currentBox = sender as TextBox;
            int index = Array.IndexOf(_boxes, currentBox);

            if (e.Key == Key.Space) { e.Handled = true; return; }

            if (e.Key == Key.Back)
            {
                if (!string.IsNullOrEmpty(currentBox.Text)) return; // Để tự xóa

                if (index > 0)
                {
                    TextBox prevBox = _boxes[index - 1];
                    prevBox.Focus();
                    Dispatcher.InvokeAsync(() => prevBox.Clear());
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Left && index > 0)
            {
                _boxes[index - 1].Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Right && index < 5)
            {
                _boxes[index + 1].Focus();
                e.Handled = true;
            }
        }

        // --- XỬ LÝ NÚT XÁC NHẬN ---

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string inputCode = "";
            foreach (var box in _boxes) inputCode += box.Text;

            // 1. Kiểm tra thiếu số
            if (inputCode.Length < 6)
            {
                ShowError("Vui lòng nhập đủ 6 số.");
                return;
            }

            // 2. Kiểm tra sai mã
            if (inputCode == _systemCode)
            {
                var parentWindow = Window.GetWindow(this) as LoginWindow;
                parentWindow?.NavigateToReset(_userEmail);
            }
            else
            {
                ShowError("Mã xác minh không chính xác");

                // Mẹo UX: Focus lại ô đầu và bôi đen (hoặc xóa trắng tuỳ bạn)
                // Ở đây mình chọn SelectAll ô đầu tiên để người dùng dễ nhập lại
                _boxes[0].Focus();
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this) as LoginWindow;
            parentWindow?.NavigateToForgotPass();
        }
    }
}