using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions; // Quan trọng: Để kiểm tra SĐT
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views
{
    public partial class AccountControl : UserControl
    {
        private bool _isEditing = false;
        private byte[] _avatarBytes = null;
        private string _currentManv = "";

        // Định nghĩa màu sắc
        private readonly Brush _grayBackground = (Brush)new BrushConverter().ConvertFrom("#F5F6F8");
        private readonly Brush _whiteBackground = Brushes.White;
        private readonly Brush _transparentBackground = Brushes.Transparent;

        // Màu viền
        private readonly Brush _defaultBorder = (Brush)new BrushConverter().ConvertFrom("#DDDDDD");
        private readonly Brush _errorBorder = Brushes.Red;

        public AccountControl()
        {
            InitializeComponent();
            _currentManv = MainWindow.CurrentMaNV;
            if (string.IsNullOrEmpty(_currentManv)) _currentManv = "NV001";
            LoadUserData();
        }

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

        // --- XỬ LÝ LƯU & KIỂM TRA LỖI ---
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. Reset lỗi cũ trước khi kiểm tra
            ResetMainErrorStyles();
            bool isValid = true;

            // 2. Kiểm tra Email (Trống hoặc sai định dạng)
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !txtEmail.Text.Contains("@") || !txtEmail.Text.Contains("."))
            {
                SetMainErrorBorder(txtEmail);
                isValid = false;
            }

            // 3. Kiểm tra SĐT (Trống hoặc không phải 10 số bắt đầu bằng 0)
            if (string.IsNullOrWhiteSpace(txtPhone.Text) || !Regex.IsMatch(txtPhone.Text, @"^0\d{9}$"))
            {
                SetMainErrorBorder(txtPhone);
                isValid = false;
            }

            // 4. Kiểm tra Địa chỉ (Trống)
            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                SetMainErrorBorder(txtAddress);
                isValid = false;
            }

            // 5. Kiểm tra Ngày sinh (Trống hoặc Ngày tương lai)
            if (dpDob.SelectedDate == null || dpDob.SelectedDate > DateTime.Now)
            {
                bdDob.BorderThickness = new Thickness(1);
                bdDob.BorderBrush = _errorBorder;
                isValid = false;
            }

            // NẾU CÓ LỖI -> HIỆN THÔNG BÁO CHUNG
            if (!isValid)
            {
                ShowMainError("Thông tin không hợp lệ");
                return;
            }

            // KHÔNG CÓ LỖI -> LƯU VÀO CSDL
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

                        // --- QUAN TRỌNG: Thoát chế độ Edit ---
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

        // --- CÁC HÀM HỖ TRỢ GIAO DIỆN ---
        private void ResetMainErrorStyles()
        {
            txbMainError.Visibility = Visibility.Collapsed;

            // Trả lại màu viền mặc định
            txtEmail.BorderBrush = _defaultBorder;
            txtPhone.BorderBrush = _defaultBorder;
            txtAddress.BorderBrush = _defaultBorder;

            // Xóa viền đỏ DatePicker
            bdDob.BorderThickness = new Thickness(0);
        }

        private void SetMainErrorBorder(TextBox txt)
        {
            txt.BorderBrush = _errorBorder; // Đổi viền sang đỏ
        }

        private void ShowMainError(string msg)
        {
            txbMainError.Text = msg;
            txbMainError.Visibility = Visibility.Visible;
        }

        // --- XỬ LÝ CHUYỂN CHẾ ĐỘ EDIT/VIEW ---
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = !_isEditing;

            if (_isEditing)
            {
                // VÀO CHẾ ĐỘ SỬA
                ResetMainErrorStyles(); // Xóa lỗi cũ nếu có

                secPassword.Visibility = Visibility.Collapsed;
                btnSave.Visibility = Visibility.Visible;
                btnChangeAvatar.Visibility = Visibility.Visible;

                // Cài đặt trạng thái các ô
                SetFieldStyle(txtHo, false, false, true); // Xám
                SetFieldStyle(txtTen, false, false, true); // Xám
                SetFieldStyle(txtEmail, true, false, false); // Trắng
                SetFieldStyle(txtPhone, true, false, false); // Trắng
                SetFieldStyle(txtAddress, true, false, false); // Trắng

                txtDobDisplay.Visibility = Visibility.Collapsed;
                dpDob.Visibility = Visibility.Visible;
            }
            else
            {
                // VỀ CHẾ ĐỘ XEM
                ResetMainErrorStyles();

                secPassword.Visibility = Visibility.Visible;
                btnSave.Visibility = Visibility.Collapsed;
                btnChangeAvatar.Visibility = Visibility.Collapsed;

                // Tất cả về trong suốt
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
                    txt.BorderBrush = _defaultBorder; // Reset màu viền về xám nhạt
                    txt.BorderThickness = new Thickness(1);
                    txt.Focusable = true;
                }
            }
        }

        // --- PHẦN ĐỔI MẬT KHẨU (View Swapping) ---
        private void btnSwitchToPassword_Click(object sender, RoutedEventArgs e)
        {
            // Reset form mật khẩu
            pbOldPass.Password = "";
            pbNewPass.Password = "";
            pbConfirmPass.Password = "";
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

            string oldPass = pbOldPass.Password;
            string newPass = pbNewPass.Password;
            string confirmPass = pbConfirmPass.Password;
            bool hasEmpty = false;

            if (string.IsNullOrEmpty(oldPass)) { pbOldPass.BorderBrush = _errorBorder; hasEmpty = true; }
            if (string.IsNullOrEmpty(newPass)) { pbNewPass.BorderBrush = _errorBorder; hasEmpty = true; }
            if (string.IsNullOrEmpty(confirmPass)) { pbConfirmPass.BorderBrush = _errorBorder; hasEmpty = true; }

            if (hasEmpty) { ShowPasswordError("Vui lòng nhập đầy đủ thông tin!"); return; }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var tk = context.Taikhoans.FirstOrDefault(x => x.Manv == _currentManv);
                    if (tk == null) return;

                    if (tk.Matkhau != oldPass)
                    {
                        pbOldPass.BorderBrush = _errorBorder;
                        ShowPasswordError("Mật khẩu hiện tại không đúng!"); return;
                    }
                    if (newPass != confirmPass)
                    {
                        pbConfirmPass.BorderBrush = _errorBorder;
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
            var brush = (Brush)new BrushConverter().ConvertFrom("#555555");
            pbOldPass.BorderBrush = brush;
            pbNewPass.BorderBrush = brush;
            pbConfirmPass.BorderBrush = brush;
            txbError.Visibility = Visibility.Collapsed;
        }

        private void ShowPasswordError(string msg)
        {
            txbError.Text = msg;
            txbError.Visibility = Visibility.Visible;
        }

        // --- CÁC HÀM PHỤ TRỢ KHÁC ---
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
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
    }
}