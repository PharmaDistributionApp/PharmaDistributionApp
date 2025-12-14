using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions; // Thêm thư viện Regex
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PharmaDistributionApp.Models;
using System.Text.RegularExpressions;

namespace PharmaDistributionApp.Views
{
    public partial class AccountControl : UserControl
    {
        private bool _isEditing = false;
        private byte[] _avatarBytes = null;
        private string _currentManv = "";

        // Màu sắc
        private readonly Brush _grayBackground = (Brush)new BrushConverter().ConvertFrom("#F5F6F8");
        private readonly Brush _whiteBackground = Brushes.White;
        private readonly Brush _transparentBackground = Brushes.Transparent;
        private readonly Brush _defaultBorder = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");
        private readonly Brush _errorBorder = Brushes.Red;

        public AccountControl()
        {
            InitializeComponent();
            _currentManv = MainWindow.CurrentMaNV;
            if (string.IsNullOrEmpty(_currentManv)) _currentManv = "NV001";
            LoadUserData();
        }

        // ... (Hàm LoadUserData giữ nguyên) ...
        private void LoadUserData()
        {
            using (var context = new QuanlyphanphoiduocphamContext())
            {
                var nv = context.Nhanviens.FirstOrDefault(x => x.Manv == _currentManv);
                if (nv != null)
                {
                    lblDisplayName.Text = nv.Tennv;
                    lblRole.Text = nv.Chucvu;

                    string fullName = nv.Tennv.Trim();
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

                    if (nv.Ngaysinh != null)
                    {
                        DateTime dob = nv.Ngaysinh.Value.ToDateTime(TimeOnly.MinValue);
                        dpDob.SelectedDate = dob;
                        txtDobDisplay.Text = dob.ToString("dd/MM/yyyy");
                    }
                    else
                    {
                        txtDobDisplay.Text = "";
                    }

                    if (nv.Avatar != null && nv.Avatar.Length > 0)
                        imgAvatar.ImageSource = LoadImage(nv.Avatar);
                }
            }
        }

        // --- XỬ LÝ NÚT LƯU THÔNG TIN (VALIDATION) ---
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. Reset trạng thái lỗi cũ (Xóa đỏ, ẩn thông báo)
            ResetMainErrorStyles();

            bool isValid = true; // Cờ kiểm tra, mặc định là đúng

            // 2. Kiểm tra Email (Không trống, phải có @ và .)
            if (string.IsNullOrWhiteSpace(txtEmail.Text) ||
                !txtEmail.Text.Contains("@") ||
                !txtEmail.Text.Contains("."))
            {
                SetMainErrorBorder(txtEmail); // Tô đỏ
                isValid = false;
            }

            // 3. Kiểm tra Số điện thoại (Không trống, đúng định dạng 10 số)
            // Regex: ^0\d{9}$ nghĩa là bắt đầu bằng 0 và theo sau là 9 chữ số
            if (string.IsNullOrWhiteSpace(txtPhone.Text) ||
                !Regex.IsMatch(txtPhone.Text, @"^0\d{9}$"))
            {
                SetMainErrorBorder(txtPhone); // Tô đỏ
                isValid = false;
            }

            // 4. Kiểm tra Địa chỉ (Không trống)
            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                SetMainErrorBorder(txtAddress); // Tô đỏ
                isValid = false;
            }

            // 5. Kiểm tra Ngày sinh
            // - Không được để trống
            // - Tuổi phải hợp lý (Ví dụ: Không được là ngày tương lai, hoặc nhỏ hơn 18 tuổi tùy logic)
            // Ở đây tôi kiểm tra: Không được Null VÀ Không được lớn hơn ngày hiện tại
            if (dpDob.SelectedDate == null || dpDob.SelectedDate > DateTime.Now)
            {
                // Tô đỏ viền Border bao quanh DatePicker
                bdDob.BorderThickness = new Thickness(1);
                bdDob.BorderBrush = _errorBorder;
                isValid = false;
            }

            // NẾU CÓ BẤT KỲ LỖI NÀO -> DỪNG VÀ BÁO LỖI
            if (!isValid)
            {
                ShowMainError("Thông tin không hợp lệ");
                return;
            }

            // DỮ LIỆU OK -> TIẾN HÀNH LƯU
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

                        // Lưu ngày sinh
                        nv.Ngaysinh = DateOnly.FromDateTime(dpDob.SelectedDate.Value);
                        txtDobDisplay.Text = dpDob.SelectedDate.Value.ToString("dd/MM/yyyy");

                        if (_avatarBytes != null) nv.Avatar = _avatarBytes;

                        context.SaveChanges();

                        // Lưu xong -> Thoát chế độ Edit ngay
                        btnEdit_Click(null, null);

                        if (_avatarBytes != null) imgAvatar.ImageSource = LoadImage(_avatarBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message);
            }
        }

        // --- CÁC HÀM HỖ TRỢ MAIN VIEW ---
        private void ResetMainErrorStyles()
        {
            // Ẩn dòng chữ báo lỗi
            txbMainError.Visibility = Visibility.Collapsed;

            // Trả lại màu viền mặc định (Xám nhạt/Trắng tùy trạng thái) cho các ô
            txtEmail.BorderBrush = _defaultBorder;
            txtPhone.BorderBrush = _defaultBorder;
            txtAddress.BorderBrush = _defaultBorder;

            // Xóa viền đỏ của DatePicker
            bdDob.BorderThickness = new Thickness(0);
        }

        private void SetMainErrorBorder(TextBox txt)
        {
            // Đổi màu viền sang Đỏ
            txt.BorderBrush = _errorBorder;
        }

        private void ShowMainError(string msg)
        {
            // Hiện dòng thông báo lỗi
            txbMainError.Text = msg;
            txbMainError.Visibility = Visibility.Visible;
        }

        // --- HÀM SET FIELD STYLE (CẬP NHẬT MỚI) ---
        private void SetFieldStyle(TextBox txt, bool isEditable, bool isViewMode = false, bool useGrayBackground = false)
        {
            txt.IsReadOnly = !isEditable;

            if (isViewMode)
            {
                // Chế độ xem: Trong suốt, không viền
                txt.Background = _transparentBackground;
                txt.BorderThickness = new Thickness(0);
            }
            else
            {
                // Chế độ sửa:
                if (useGrayBackground)
                {
                    txt.Background = _grayBackground;
                    txt.BorderThickness = new Thickness(0);
                    txt.Focusable = false;
                }
                else
                {
                    txt.Background = _whiteBackground;
                    // Reset màu viền về mặc định mỗi khi bấm Edit lại
                    txt.BorderBrush = _defaultBorder;
                    txt.BorderThickness = new Thickness(1);
                    txt.Focusable = true;
                }
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = !_isEditing;

            if (_isEditing)
            {
                // Khi bắt đầu sửa: Reset lỗi cũ
                ResetMainErrorStyles();

                // ... (Code cũ hiển thị nút Lưu, ẩn Password...)
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
                // Khi Hủy bỏ / Lưu xong: Reset lỗi
                ResetMainErrorStyles();

                // ... (Code cũ ẩn nút Lưu, hiện Password...)
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

                if (dpDob.SelectedDate.HasValue)
                    txtDobDisplay.Text = dpDob.SelectedDate.Value.ToString("dd/MM/yyyy");
            }
        }

        // ... (Phần code PasswordView, LoadImage, btnChangeAvatar... giữ nguyên như file cũ) ...
        // Bạn copy lại các hàm: btnSwitchToPassword_Click, btnCancelPassword_Click, 
        // btnSavePassword_Click, ResetErrorStyles, SetErrorBorder, ShowError, btnChangeAvatar_Click, LoadImage
        // từ câu trả lời trước vào đây nhé.
        private void btnSwitchToPassword_Click(object sender, RoutedEventArgs e)
        {
            pbOldPass.Password = ""; pbNewPass.Password = ""; pbConfirmPass.Password = "";
            ResetErrorStyles(); // Hàm này của PasswordView
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
            // Reset Error PasswordView
            pbOldPass.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#555555");
            pbNewPass.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#555555");
            pbConfirmPass.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#555555");
            txbError.Visibility = Visibility.Collapsed;

            string oldPass = pbOldPass.Password;
            string newPass = pbNewPass.Password;
            string confirmPass = pbConfirmPass.Password;
            bool hasEmpty = false;

            if (string.IsNullOrEmpty(oldPass)) { pbOldPass.BorderBrush = Brushes.Red; hasEmpty = true; }
            if (string.IsNullOrEmpty(newPass)) { pbNewPass.BorderBrush = Brushes.Red; hasEmpty = true; }
            if (string.IsNullOrEmpty(confirmPass)) { pbConfirmPass.BorderBrush = Brushes.Red; hasEmpty = true; }

            if (hasEmpty) { txbError.Text = "Vui lòng nhập đầy đủ thông tin!"; txbError.Visibility = Visibility.Visible; return; }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var tk = context.Taikhoans.FirstOrDefault(x => x.Manv == _currentManv);
                    if (tk == null) return;

                    if (tk.Matkhau != oldPass)
                    {
                        pbOldPass.BorderBrush = Brushes.Red;
                        txbError.Text = "Mật khẩu hiện tại không đúng!"; txbError.Visibility = Visibility.Visible; return;
                    }
                    if (newPass != confirmPass)
                    {
                        pbConfirmPass.BorderBrush = Brushes.Red;
                        txbError.Text = "Mật khẩu xác nhận không khớp!"; txbError.Visibility = Visibility.Visible; return;
                    }

                    tk.Matkhau = newPass;
                    context.SaveChanges();
                    MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo");
                    btnCancelPassword_Click(null, null);
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void ResetErrorStyles()
        {
            var defaultBrush = (Brush)new BrushConverter().ConvertFrom("#555555");
            pbOldPass.BorderBrush = defaultBrush;
            pbNewPass.BorderBrush = defaultBrush;
            pbConfirmPass.BorderBrush = defaultBrush;
            txbError.Visibility = Visibility.Collapsed;
        }

        private void btnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Filter = "Image files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
            if (op.ShowDialog() == true)
            {
                imgAvatar.ImageSource = new BitmapImage(new Uri(op.FileName));
                _avatarBytes = File.ReadAllBytes(op.FileName);
            }
        }
        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0; image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null; image.StreamSource = mem; image.EndInit();
            }
            image.Freeze(); return image;
        }
    }
}