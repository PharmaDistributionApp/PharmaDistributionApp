using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf; // Cần thiết để chỉnh Icon
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views
{
    public partial class AccountControl : UserControl
    {
        private bool _isEditing = false;
        private byte[] _avatarBytes = null;
        private string _currentManv = "";
        private readonly Brush _grayBackground = (Brush)new BrushConverter().ConvertFrom("#F5F6F8");
        private readonly Brush _whiteBackground = Brushes.White;
        private readonly Brush _transparentBackground = Brushes.Transparent;
        private readonly Brush _defaultBorder = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");
        private readonly Brush _passwordBorder = (Brush)new BrushConverter().ConvertFrom("#555555");
        private readonly Brush _errorBorder = Brushes.Red;

        public AccountControl()
        {
            InitializeComponent();
            // Get the current MainWindow instance and access CurrentMaNV
            var mainWindow = Application.Current.MainWindow as MainWindow;
            _currentManv = mainWindow?.CurrentUser?.Manv;
            if (string.IsNullOrEmpty(_currentManv)) _currentManv = "NV001";
            LoadUserData();
        }

        // ==========================================
        // PHẦN XỬ LÝ ĐỔI MẬT KHẨU + CON MẮT (MỚI)
        // ==========================================

        private void ToggleEye_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            string tag = btn.Tag.ToString(); // "Old", "New", hoặc "Confirm"

            // Tìm các control tương ứng dựa trên Tag
            PasswordBox pb = FindName($"pb{tag}Pass") as PasswordBox;
            TextBox txt = FindName($"txt{tag}Pass") as TextBox;
            PackIcon? icon = FindName($"iconEye{tag}") as PackIcon;

            if (pb != null && txt != null && icon != null)
            {
                if (pb.Visibility == Visibility.Visible)
                {
                    // Đang ẩn -> Chuyển sang hiện
                    txt.Text = pb.Password; // Copy pass sang text
                    pb.Visibility = Visibility.Collapsed;
                    txt.Visibility = Visibility.Visible;
                    icon.Kind = PackIconKind.Eye; // Đổi icon mở mắt
                }
                else
                {
                    // Đang hiện -> Chuyển sang ẩn
                    pb.Password = txt.Text; // Copy text về lại pass
                    txt.Visibility = Visibility.Collapsed;
                    pb.Visibility = Visibility.Visible;
                    icon.Kind = PackIconKind.EyeOff; // Đổi icon nhắm mắt
                }
            }
        }

        // Helper lấy giá trị mật khẩu (Dù đang ở chế độ ẩn hay hiện)
        private string GetPasswordValue(string tag)
        {
            PasswordBox pb = FindName($"pb{tag}Pass") as PasswordBox;
            TextBox txt = FindName($"txt{tag}Pass") as TextBox;

            // Nếu TextBox đang hiện thì lấy giá trị từ TextBox, ngược lại lấy từ PasswordBox
            if (txt != null && txt.Visibility == Visibility.Visible)
                return txt.Text;

            return pb != null ? pb.Password : "";
        }

        // Helper tô viền đỏ (Xử lý cho cả 2 ô)
        private void SetPasswordError(string tag)
        {
            PasswordBox pb = FindName($"pb{tag}Pass") as PasswordBox;
            TextBox txt = FindName($"txt{tag}Pass") as TextBox;

            if (pb != null) pb.BorderBrush = _errorBorder;
            if (txt != null) txt.BorderBrush = _errorBorder;
        }

        private void btnSavePassword_Click(object sender, RoutedEventArgs e)
        {
            ResetPasswordErrorStyles();

            // Lấy dữ liệu thông qua hàm Helper (để đảm bảo lấy đúng cái người dùng đang nhập)
            string oldPass = GetPasswordValue("Old");
            string newPass = GetPasswordValue("New");
            string confirmPass = GetPasswordValue("Confirm");

            bool hasEmpty = false;

            if (string.IsNullOrEmpty(oldPass)) { SetPasswordError("Old"); hasEmpty = true; }
            if (string.IsNullOrEmpty(newPass)) { SetPasswordError("New"); hasEmpty = true; }
            if (string.IsNullOrEmpty(confirmPass)) { SetPasswordError("Confirm"); hasEmpty = true; }

            if (hasEmpty) { ShowPasswordError("Vui lòng nhập đầy đủ thông tin!"); return; }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var tk = context.Taikhoans.FirstOrDefault(x => x.Manv == _currentManv);
                    if (tk == null) return;

                    if (tk.Matkhau != oldPass)
                    {
                        SetPasswordError("Old");
                        ShowPasswordError("Mật khẩu hiện tại không đúng!"); return;
                    }
                    if (newPass != confirmPass)
                    {
                        SetPasswordError("Confirm");
                        ShowPasswordError("Mật khẩu xác nhận không khớp!"); return;
                    }

                    tk.Matkhau = newPass;
                    context.SaveChanges();
                    MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo");
                    btnCancelPassword_Click(null, null);
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void ResetPasswordErrorStyles()
        {
            // Reset màu viền cho cả PasswordBox và TextBox
            pbOldPass.BorderBrush = _passwordBorder; txtOldPass.BorderBrush = _passwordBorder;
            pbNewPass.BorderBrush = _passwordBorder; txtNewPass.BorderBrush = _passwordBorder;
            pbConfirmPass.BorderBrush = _passwordBorder; txtConfirmPass.BorderBrush = _passwordBorder;

            txbError.Visibility = Visibility.Collapsed;
        }

        private void btnSwitchToPassword_Click(object sender, RoutedEventArgs e)
        {
            // Reset dữ liệu và trạng thái hiển thị
            pbOldPass.Password = ""; txtOldPass.Text = "";
            pbNewPass.Password = ""; txtNewPass.Text = "";
            pbConfirmPass.Password = ""; txtConfirmPass.Text = "";

            // Đưa tất cả về chế độ ẩn (PasswordBox hiện, TextBox ẩn, Icon EyeOff)
            ResetToHiddenMode("Old");
            ResetToHiddenMode("New");
            ResetToHiddenMode("Confirm");

            ResetPasswordErrorStyles();
            MainView.Visibility = Visibility.Collapsed;
            PasswordView.Visibility = Visibility.Visible;
        }

        private void ResetToHiddenMode(string tag)
        {
            PasswordBox pb = FindName($"pb{tag}Pass") as PasswordBox;
            TextBox txt = FindName($"txt{tag}Pass") as TextBox;
            PackIcon? icon = FindName($"iconEye{tag}") as PackIcon;

            if (pb != null) pb.Visibility = Visibility.Visible;
            if (txt != null) txt.Visibility = Visibility.Collapsed;
            if (icon != null) icon.Kind = PackIconKind.EyeOff;
        }

        private void btnCancelPassword_Click(object sender, RoutedEventArgs e)
        {
            PasswordView.Visibility = Visibility.Collapsed;
            MainView.Visibility = Visibility.Visible;
        }

        private void ShowPasswordError(string msg) { txbError.Text = msg; txbError.Visibility = Visibility.Visible; }

        // ... (Các hàm btnChangeAvatar_Click, LoadImage giữ nguyên) ...
        private void btnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
            if (op.ShowDialog() == true)
            {
                try { imgAvatar.ImageSource = new BitmapImage(new Uri(op.FileName)); _avatarBytes = File.ReadAllBytes(op.FileName); }
                catch { MessageBox.Show("Ảnh lỗi!"); }
            }
        }
        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage(); using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0; image.BeginInit(); image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad; image.UriSource = null; image.StreamSource = mem; image.EndInit();
            }
            image.Freeze(); return image;
        }

        // ... (Đừng quên copy lại các hàm LoadUserData, btnSave_Click... từ code cũ nếu chưa có) ...
        // ... Để code chạy được, bạn cần paste lại phần logic Edit Info vào class này ...
        private void LoadUserData()
        {
            using (var context = new QuanlyphanphoiduocphamContext())
            {
                var nv = context.Nhanviens.FirstOrDefault(x => x.Manv == _currentManv);
                if (nv != null)
                {
                    lblDisplayName.Text = nv.Tennv; lblRole.Text = nv.Chucvu;
                    string fullName = nv.Tennv.Trim(); int spaceIndex = fullName.LastIndexOf(' ');
                    if (spaceIndex > 0) { txtHo.Text = fullName.Substring(0, spaceIndex); txtTen.Text = fullName.Substring(spaceIndex + 1); }
                    else { txtHo.Text = fullName; txtTen.Text = ""; }
                    txtEmail.Text = nv.Email; txtPhone.Text = nv.Sdt; txtAddress.Text = nv.Diachi;
                    if (nv.Ngaysinh != null) { DateTime dob = nv.Ngaysinh.Value.ToDateTime(TimeOnly.MinValue); dpDob.SelectedDate = dob; txtDobDisplay.Text = dob.ToString("dd/MM/yyyy"); }
                    else { txtDobDisplay.Text = ""; }
                    if (nv.Avatar != null && nv.Avatar.Length > 0) imgAvatar.ImageSource = LoadImage(nv.Avatar);
                }
            }
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            ResetMainErrorStyles(); bool isValid = true;
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains("@") || !txtEmail.Text.Contains(".")) { txtEmail.BorderBrush = _errorBorder; isValid = false; }
            if (string.IsNullOrWhiteSpace(txtPhone.Text) || !Regex.IsMatch(txtPhone.Text, @"^0\d{9}$")) { txtPhone.BorderBrush = _errorBorder; isValid = false; }
            if (string.IsNullOrWhiteSpace(txtAddress.Text)) { txtAddress.BorderBrush = _errorBorder; isValid = false; }
            if (dpDob.SelectedDate == null || dpDob.SelectedDate > DateTime.Now) { bdDob.BorderThickness = new Thickness(1); bdDob.BorderBrush = _errorBorder; isValid = false; }
            if (!isValid) { txbMainError.Text = "Thông tin không hợp lệ"; txbMainError.Visibility = Visibility.Visible; return; }
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var nv = context.Nhanviens.FirstOrDefault(x => x.Manv == _currentManv);
                    if (nv != null)
                    {
                        nv.Email = txtEmail.Text; nv.Sdt = txtPhone.Text; nv.Diachi = txtAddress.Text;
                        nv.Ngaysinh = DateOnly.FromDateTime(dpDob.SelectedDate.Value); txtDobDisplay.Text = dpDob.SelectedDate.Value.ToString("dd/MM/yyyy");
                        if (_avatarBytes != null) nv.Avatar = _avatarBytes;
                        context.SaveChanges(); btnEdit_Click(null, null);
                        if (_avatarBytes != null) imgAvatar.ImageSource = LoadImage(_avatarBytes);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi hệ thống: " + ex.Message); }
        }
        private void ResetMainErrorStyles() { txbMainError.Visibility = Visibility.Collapsed; txtEmail.BorderBrush = _defaultBorder; txtPhone.BorderBrush = _defaultBorder; txtAddress.BorderBrush = _defaultBorder; bdDob.BorderThickness = new Thickness(0); }
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = !_isEditing;
            if (_isEditing)
            {
                ResetMainErrorStyles(); secPassword.Visibility = Visibility.Collapsed; btnSave.Visibility = Visibility.Visible; btnChangeAvatar.Visibility = Visibility.Visible;
                SetFieldStyle(txtHo, false, false, true); SetFieldStyle(txtTen, false, false, true); SetFieldStyle(txtEmail, true, false, false); SetFieldStyle(txtPhone, true, false, false); SetFieldStyle(txtAddress, true, false, false);
                txtDobDisplay.Visibility = Visibility.Collapsed; dpDob.Visibility = Visibility.Visible;
            }
            else
            {
                ResetMainErrorStyles(); secPassword.Visibility = Visibility.Visible; btnSave.Visibility = Visibility.Collapsed; btnChangeAvatar.Visibility = Visibility.Collapsed;
                SetFieldStyle(txtHo, false, true, false); SetFieldStyle(txtTen, false, true, false); SetFieldStyle(txtEmail, false, true, false); SetFieldStyle(txtPhone, false, true, false); SetFieldStyle(txtAddress, false, true, false);
                txtDobDisplay.Visibility = Visibility.Visible; dpDob.Visibility = Visibility.Collapsed;
                if (dpDob.SelectedDate.HasValue) txtDobDisplay.Text = dpDob.SelectedDate.Value.ToString("dd/MM/yyyy");
            }
        }
        private void SetFieldStyle(TextBox txt, bool isEditable, bool isViewMode = false, bool useGrayBackground = false)
        {
            txt.IsReadOnly = !isEditable;
            if (isViewMode) { txt.Background = _transparentBackground; txt.BorderThickness = new Thickness(0); }
            else
            {
                if (useGrayBackground) { txt.Background = _grayBackground; txt.BorderThickness = new Thickness(0); txt.Focusable = false; }
                else { txt.Background = _whiteBackground; txt.BorderBrush = _defaultBorder; txt.BorderThickness = new Thickness(1); txt.Focusable = true; }
            }
        }
    }
}