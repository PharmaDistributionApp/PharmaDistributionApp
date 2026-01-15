using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf;
using PharmaDistributionApp.Models; // Nơi chứa UserSession và Context

namespace PharmaDistributionApp.Views
{
    public partial class AccountControl : UserControl
    {
        private bool _isEditing = false;
        private byte[] _avatarBytes = null;
        private string _currentManv = "";

        // Màu sắc giao diện
        private readonly Brush _grayBackground = (Brush)new BrushConverter().ConvertFrom("#F5F6F8");
        private readonly Brush _whiteBackground = Brushes.White;
        private readonly Brush _transparentBackground = Brushes.Transparent;
        private readonly Brush _defaultBorder = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");
        private readonly Brush _passwordBorder = (Brush)new BrushConverter().ConvertFrom("#555555");
        private readonly Brush _errorBorder = Brushes.Red;

        public AccountControl()
        {
            InitializeComponent();

            // [QUAN TRỌNG]: Đăng ký sự kiện Loaded để cập nhật lại dữ liệu mỗi khi mở tab
            this.Loaded += AccountControl_Loaded;
        }

        // Sự kiện chạy mỗi khi UserControl hiển thị lên màn hình
        private void AccountControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Reset giao diện về mặc định (tắt chế độ sửa/đổi pass)
            if (_isEditing) btnEdit_Click(null, null);
            btnCancelPassword_Click(null, null);

            // 2. Lấy mã nhân viên từ SESSION TĨNH (Đảm bảo chính xác người đang login)
            if (!string.IsNullOrEmpty(UserSession.CurrentMaNV))
            {
                _currentManv = UserSession.CurrentMaNV;
            }
            else
            {
                // Fallback chỉ dùng khi chạy debug giao diện mà không qua Login
                _currentManv = "NV01";
            }

            // 3. Tải dữ liệu từ DB
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var nv = context.Nhanviens.FirstOrDefault(x => x.Manv == _currentManv);

                    if (nv != null)
                    {
                        // --- 1. Load thông tin Text ---
                        lblDisplayName.Text = nv.Tennv;
                        lblRole.Text = nv.Chucvu;

                        string fullName = nv.Tennv != null ? nv.Tennv.Trim() : "";
                        int spaceIndex = fullName.LastIndexOf(' ');
                        if (spaceIndex > 0)
                        {
                            txtHo.Text = fullName.Substring(0, spaceIndex);
                            txtTen.Text = fullName.Substring(spaceIndex + 1);
                        }
                        else
                        {
                            txtHo.Text = fullName;
                            txtTen.Text = "";
                        }

                        txtEmail.Text = nv.Email;
                        txtPhone.Text = nv.Sdt;
                        txtAddress.Text = nv.Diachi;

                        // --- 2. Load Ngày sinh ---
                        if (nv.Ngaysinh != null)
                        {
                            DateTime dob = nv.Ngaysinh.Value.ToDateTime(TimeOnly.MinValue);
                            dpDob.SelectedDate = dob;
                            txtDobDisplay.Text = dob.ToString("dd/MM/yyyy");
                        }
                        else
                        {
                            txtDobDisplay.Text = "Chưa cập nhật";
                            dpDob.SelectedDate = null;
                        }

                        // --- 3. XỬ LÝ AVATAR (LOGIC ĐÃ SỬA) ---
                        if (nv.Avatar != null && nv.Avatar.Length > 0)
                        {
                            // Có ảnh riêng trong DB -> Load lên
                            imgAvatar.ImageSource = LoadImage(nv.Avatar);
                            _avatarBytes = nv.Avatar;
                        }
                        else
                        {
                            // Không có ảnh -> Gọi hàm load ảnh mặc định
                            LoadDefaultAvatar();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("resource"))
                    MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        // --- HÀM LOAD ẢNH MẶC ĐỊNH (ĐÃ SỬA ĐƯỜNG DẪN) ---
        private void LoadDefaultAvatar()
        {
            _avatarBytes = null; // Reset byte ảnh
            try
            {
                // SỬA LỖI QUAN TRỌNG:
                // Dùng Pack URI đầy đủ để tìm ảnh đã được nhúng (Build Action = Resource)
                // Cú pháp: pack://application:,,,/Tên_Project;component/Đường_dẫn_từ_gốc_Project

                var uri = new Uri("pack://application:,,,/PharmaDistributionApp;component/Images/default_avatar.jpg");

                // Tạo BitmapImage từ URI này
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imgAvatar.ImageSource = bitmap;
            }
            catch (Exception ex)
            {
                // Nếu vẫn lỗi, vẽ hình tròn màu xám để giao diện không bị xấu
                // MessageBox.Show("Lỗi ảnh: " + ex.Message); // Bật dòng này nếu muốn xem lỗi chi tiết
                imgAvatar.ImageSource = CreatePlaceholderImage();
            }
        }

        // Hàm tạo ảnh giả (Placeholder) khi file ảnh bị lỗi hoặc không tồn tại
        private ImageSource CreatePlaceholderImage()
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(100, 100, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                // Vẽ hình tròn xám
                context.DrawEllipse(Brushes.LightGray, null, new Point(50, 50), 50, 50);

                // Vẽ chữ cái đầu của tên
                string letter = !string.IsNullOrEmpty(txtTen.Text) ? txtTen.Text.Substring(0, 1).ToUpper() : "U";
                FormattedText text = new FormattedText(letter,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    40, Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                context.DrawText(text, new Point(50 - text.Width / 2, 50 - text.Height / 2));
            }
            bitmap.Render(visual);
            return bitmap;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            ResetMainErrorStyles();
            bool isValid = true;

            // --- Validation ---
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains("@"))
            { txtEmail.BorderBrush = _errorBorder; isValid = false; }
            if (string.IsNullOrWhiteSpace(txtPhone.Text) || !Regex.IsMatch(txtPhone.Text, @"^0\d{9}$"))
            { txtPhone.BorderBrush = _errorBorder; isValid = false; }
            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            { txtAddress.BorderBrush = _errorBorder; isValid = false; }
            if (dpDob.SelectedDate == null || dpDob.SelectedDate > DateTime.Now)
            { bdDob.BorderBrush = _errorBorder; isValid = false; }

            if (!isValid)
            {
                txbMainError.Text = "Thông tin không hợp lệ";
                txbMainError.Visibility = Visibility.Visible;
                return;
            }

            // --- Lưu xuống DB ---
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var nv = context.Nhanviens.FirstOrDefault(x => x.Manv == _currentManv);
                    if (nv != null)
                    {
                        nv.Email = txtEmail.Text;
                        nv.Sdt = txtPhone.Text;
                        nv.Diachi = txtAddress.Text;

                        // Chuyển DateTime (DatePicker) -> DateOnly (Model)
                        if (dpDob.SelectedDate.HasValue)
                        {
                            nv.Ngaysinh = DateOnly.FromDateTime(dpDob.SelectedDate.Value);
                        }

                        if (_avatarBytes != null) nv.Avatar = _avatarBytes;

                        context.SaveChanges();

                        // Tắt chế độ sửa và refresh giao diện
                        btnEdit_Click(null, null);
                        if (_avatarBytes != null) imgAvatar.ImageSource = LoadImage(_avatarBytes);
                        if (dpDob.SelectedDate.HasValue) txtDobDisplay.Text = dpDob.SelectedDate.Value.ToString("dd/MM/yyyy");

                        MessageBox.Show("Cập nhật thành công!", "Thông báo");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu: " + ex.Message);
            }
        }

        // ==========================================================
        // CÁC HÀM HỖ TRỢ GIAO DIỆN (VIEW LOGIC)
        // ==========================================================

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = !_isEditing;

            if (_isEditing)
            {
                // Bật chế độ sửa
                ResetMainErrorStyles();
                secPassword.Visibility = Visibility.Collapsed;
                btnSave.Visibility = Visibility.Visible;
                btnChangeAvatar.Visibility = Visibility.Visible;

                SetFieldStyle(txtHo, false, false, true);
                SetFieldStyle(txtTen, false, false, true);
                SetFieldStyle(txtEmail, true, false, false);
                SetFieldStyle(txtPhone, true, false, false);
                SetFieldStyle(txtAddress, true, false, false);

                txtDobDisplay.Visibility = Visibility.Collapsed;
                dpDob.Visibility = Visibility.Visible;
            }
            else
            {
                // Tắt chế độ sửa
                ResetMainErrorStyles();
                secPassword.Visibility = Visibility.Visible;
                btnSave.Visibility = Visibility.Collapsed;
                btnChangeAvatar.Visibility = Visibility.Collapsed;

                SetFieldStyle(txtHo, false, true, false);
                SetFieldStyle(txtTen, false, true, false);
                SetFieldStyle(txtEmail, false, true, false);
                SetFieldStyle(txtPhone, false, true, false);
                SetFieldStyle(txtAddress, false, true, false);

                txtDobDisplay.Visibility = Visibility.Visible;
                dpDob.Visibility = Visibility.Collapsed;

                // Revert hiển thị ngày nếu hủy bỏ
                if (dpDob.SelectedDate.HasValue)
                    txtDobDisplay.Text = dpDob.SelectedDate.Value.ToString("dd/MM/yyyy");
            }
        }

        private void SetFieldStyle(TextBox txt, bool isEditable, bool isViewMode = false, bool useGrayBackground = false)
        {
            txt.IsReadOnly = !isEditable;
            if (isViewMode)
            {
                txt.Background = _transparentBackground;
                txt.BorderThickness = new Thickness(0);
            }
            else
            {
                if (useGrayBackground)
                {
                    txt.Background = _grayBackground;
                    txt.BorderThickness = new Thickness(0);
                    txt.Focusable = false;
                }
                else
                {
                    txt.Background = _whiteBackground;
                    txt.BorderBrush = _defaultBorder;
                    txt.BorderThickness = new Thickness(1);
                    txt.Focusable = true;
                }
            }
        }

        private void ResetMainErrorStyles()
        {
            txbMainError.Visibility = Visibility.Collapsed;
            txtEmail.BorderBrush = _defaultBorder;
            txtPhone.BorderBrush = _defaultBorder;
            txtAddress.BorderBrush = _defaultBorder;
            bdDob.BorderThickness = new Thickness(0);
        }

        private void btnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
            if (op.ShowDialog() == true)
            {
                try
                {
                    var uri = new Uri(op.FileName);
                    imgAvatar.ImageSource = new BitmapImage(uri);
                    _avatarBytes = File.ReadAllBytes(op.FileName);
                }
                catch { MessageBox.Show("Ảnh lỗi!"); }
            }
        }

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        // ==========================================================
        // XỬ LÝ ĐỔI MẬT KHẨU
        // ==========================================================

        private void btnSwitchToPassword_Click(object sender, RoutedEventArgs e)
        {
            pbOldPass.Password = ""; txtOldPass.Text = "";
            pbNewPass.Password = ""; txtNewPass.Text = "";
            pbConfirmPass.Password = ""; txtConfirmPass.Text = "";
            ResetToHiddenMode("Old"); ResetToHiddenMode("New"); ResetToHiddenMode("Confirm");
            ResetPasswordErrorStyles();
            MainView.Visibility = Visibility.Collapsed;
            PasswordView.Visibility = Visibility.Visible;
        }

        private void btnCancelPassword_Click(object sender, RoutedEventArgs e)
        {
            PasswordView.Visibility = Visibility.Collapsed;
            MainView.Visibility = Visibility.Visible;
        }

        private void btnSavePassword_Click(object sender, RoutedEventArgs e)
        {
            ResetPasswordErrorStyles();
            string oldPass = GetPasswordValue("Old");
            string newPass = GetPasswordValue("New");
            string confirmPass = GetPasswordValue("Confirm");

            if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
            { ShowPasswordError("Vui lòng nhập đủ thông tin"); return; }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var tk = context.Taikhoans.FirstOrDefault(x => x.Manv == _currentManv);
                    if (tk == null) return;

                    if (tk.Matkhau != oldPass) { ShowPasswordError("Mật khẩu cũ không đúng"); return; }
                    if (newPass != confirmPass) { ShowPasswordError("Mật khẩu xác nhận không khớp"); return; }

                    tk.Matkhau = newPass;
                    context.SaveChanges();
                    MessageBox.Show("Đổi mật khẩu thành công!");
                    btnCancelPassword_Click(null, null);
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void ToggleEye_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button; if (btn == null) return;
            string tag = btn.Tag.ToString();
            PasswordBox pb = FindName($"pb{tag}Pass") as PasswordBox;
            TextBox txt = FindName($"txt{tag}Pass") as TextBox;
            PackIcon? icon = FindName($"iconEye{tag}") as PackIcon;

            if (pb != null && txt != null && icon != null)
            {
                if (pb.Visibility == Visibility.Visible) { txt.Text = pb.Password; pb.Visibility = Visibility.Collapsed; txt.Visibility = Visibility.Visible; icon.Kind = PackIconKind.Eye; }
                else { pb.Password = txt.Text; txt.Visibility = Visibility.Collapsed; pb.Visibility = Visibility.Visible; icon.Kind = PackIconKind.EyeOff; }
            }
        }
        private string GetPasswordValue(string tag)
        {
            PasswordBox pb = FindName($"pb{tag}Pass") as PasswordBox;
            TextBox txt = FindName($"txt{tag}Pass") as TextBox;
            return (txt != null && txt.Visibility == Visibility.Visible) ? txt.Text : (pb != null ? pb.Password : "");
        }
        private void ResetToHiddenMode(string tag)
        {
            PasswordBox pb = FindName($"pb{tag}Pass") as PasswordBox; TextBox txt = FindName($"txt{tag}Pass") as TextBox; PackIcon? icon = FindName($"iconEye{tag}") as PackIcon;
            if (pb != null) pb.Visibility = Visibility.Visible; if (txt != null) txt.Visibility = Visibility.Collapsed; if (icon != null) icon.Kind = PackIconKind.EyeOff;
        }
        private void ResetPasswordErrorStyles() { txbError.Visibility = Visibility.Collapsed; }
        private void ShowPasswordError(string msg) { txbError.Text = msg; txbError.Visibility = Visibility.Visible; }
    }
}