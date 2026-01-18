using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MaterialDesignThemes.Wpf;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;

namespace PharmaDistributionApp.Views
{
    public partial class AccountControl : UserControl
    {
        private bool _isEditing = false;
        private byte[] _avatarBytes = null;
        private string _currentManv = "";

        public AccountControl()
        {
            InitializeComponent();
            if (UserSession.IsLoggedIn && UserSession.CurrentUser != null)
                _currentManv = UserSession.CurrentUser.Manv;
            else
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                _currentManv = mainWindow?.CurrentMaNV ?? "NV001";
            }
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
                        lblDisplayName.Text = nv.Tennv;
                        lblRole.Text = nv.Chucvu ?? "Nhân viên";

                        // Tách tên hiển thị
                        string fullName = nv.Tennv.Trim();
                        int spaceIndex = fullName.LastIndexOf(' ');
                        if (spaceIndex > 0) { txtHo.Text = fullName.Substring(0, spaceIndex); txtTen.Text = fullName.Substring(spaceIndex + 1); }
                        else { txtHo.Text = fullName; txtTen.Text = ""; }

                        txtEmail.Text = nv.Email;
                        txtPhone.Text = nv.Sdt;
                        txtAddress.Text = nv.Diachi;

                        // [GIẢI PHÁP TRIỆT ĐỂ]: Dùng trực tiếp DateTime? cho DatePicker
                        dpDob.SelectedDate = nv.Ngaysinh;
                        txtDobDisplay.Text = nv.Ngaysinh?.ToString("dd/MM/yyyy") ?? "";

                        if (nv.Avatar != null && nv.Avatar.Length > 0)
                        {
                            imgAvatar.ImageSource = LoadImage(nv.Avatar);
                            _avatarBytes = nv.Avatar;
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi hệ thống: " + ex.Message); }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
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
                        // Gán trực tiếp vì cả hai đều là DateTime?
                        nv.Ngaysinh = dpDob.SelectedDate;

                        if (_avatarBytes != null) nv.Avatar = _avatarBytes;
                        context.SaveChanges();
                        if (UserSession.CurrentUser != null && UserSession.CurrentUser.Manv == _currentManv) UserSession.CurrentUser = nv;

                        MessageBox.Show("Cập nhật thành công!");
                        _isEditing = false;
                        ToggleEditUI(false);
                        LoadUserData();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi khi lưu: " + ex.Message); }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            _isEditing = !_isEditing;
            ToggleEditUI(_isEditing);
            if (!_isEditing) LoadUserData();
        }

        private void ToggleEditUI(bool isEditing)
        {
            secPassword.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            btnSave.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            btnChangeAvatar.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
            txtDobDisplay.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            dpDob.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;

            txtEmail.IsReadOnly = !isEditing;
            txtPhone.IsReadOnly = !isEditing;
            txtAddress.IsReadOnly = !isEditing;
            // Họ tên thường không cho tự sửa để tránh sai lệch hồ sơ nhân sự
        }

        // --- CÁC HÀM XỬ LÝ MẬT KHẨU (BỔ SUNG ĐỂ XÓA LỖI CS1061) ---
        private void btnSwitchToPassword_Click(object sender, RoutedEventArgs e)
        {
            MainView.Visibility = Visibility.Collapsed;
            PasswordView.Visibility = Visibility.Visible;
        }

        private void btnCancelPassword_Click(object sender, RoutedEventArgs e)
        {
            PasswordView.Visibility = Visibility.Collapsed;
            MainView.Visibility = Visibility.Visible;
        }

        private void ToggleEye_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag == null) return;
            string tag = btn.Tag.ToString();
            PasswordBox pb = FindName($"pb{tag}Pass") as PasswordBox;
            TextBox txt = FindName($"txt{tag}Pass") as TextBox;
            PackIcon icon = FindName($"iconEye{tag}") as PackIcon;

            if (pb != null && txt != null && icon != null)
            {
                bool isVisible = pb.Visibility == Visibility.Visible;
                txt.Text = pb.Password;
                pb.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
                txt.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                icon.Kind = isVisible ? PackIconKind.Eye : PackIconKind.EyeOff;
            }
        }

        private void btnSavePassword_Click(object sender, RoutedEventArgs e)
        {
            string oldP = pbOldPass.Visibility == Visibility.Visible ? pbOldPass.Password : txtOldPass.Text;
            string newP = pbNewPass.Visibility == Visibility.Visible ? pbNewPass.Password : txtNewPass.Text;
            string conf = pbConfirmPass.Visibility == Visibility.Visible ? pbConfirmPass.Password : txtConfirmPass.Text;

            if (string.IsNullOrEmpty(newP) || newP != conf) { MessageBox.Show("Mật khẩu không khớp!"); return; }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var tk = context.Taikhoans.FirstOrDefault(x => x.Manv == _currentManv);
                    if (tk != null && tk.Matkhau == oldP)
                    {
                        tk.Matkhau = newP;
                        context.SaveChanges();
                        MessageBox.Show("Đổi mật khẩu thành công!");
                        btnCancelPassword_Click(null, null);
                    }
                    else MessageBox.Show("Mật khẩu cũ không đúng!");
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void btnChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog { Filter = "Images|*.jpg;*.png" };
            if (op.ShowDialog() == true)
            {
                imgAvatar.ImageSource = new BitmapImage(new Uri(op.FileName));
                _avatarBytes = File.ReadAllBytes(op.FileName);
            }
        }

        private static BitmapImage LoadImage(byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            var img = new BitmapImage();
            using (var m = new MemoryStream(data)) { img.BeginInit(); img.StreamSource = m; img.CacheOption = BitmapCacheOption.OnLoad; img.EndInit(); }
            return img;
        }
        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { }
    }
}