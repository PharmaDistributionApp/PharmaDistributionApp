using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views.KhachHangView
{
    public partial class ThemSuaKhachHangWindow : Window
    {
        private Khachhang _currentKH;
        private bool _isEditMode = false;

        private readonly Brush _errorBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
        private readonly Brush _defaultBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#89000000"));

        // Constructor Thêm mới
        public ThemSuaKhachHangWindow()
        {
            InitializeComponent();
            SetupWindowLogic();
            _isEditMode = false;
            lblTitle.Text = "THÊM KHÁCH HÀNG MỚI";
            GenerateNewKHCode();
        }

        // Constructor Chỉnh sửa
        public ThemSuaKhachHangWindow(Khachhang kh)
        {
            InitializeComponent();
            SetupWindowLogic();
            _isEditMode = true;
            _currentKH = kh;
            lblTitle.Text = "CẬP NHẬT THÔNG TIN";
            LoadDataToForm();
        }

        private void SetupWindowLogic()
        {
            this.KeyDown += (s, e) => {
                if (e.Key == Key.Escape) this.Close();
                else if (e.Key == Key.Enter) BtnLuu_Click(null, null);
            };

            this.Loaded += (s, e) => txtTenKH.Focus();

            // Clear error events
            txtTenKH.TextChanged += (s, e) => ClearSingleError(txtTenKH, errTenKH);
            txtSdt.TextChanged += (s, e) => ClearSingleError(txtSdt, errSdt);
            txtEmail.TextChanged += (s, e) => ClearSingleError(txtEmail, errEmail);
            txtDiaChi.TextChanged += (s, e) => ClearSingleError(txtDiaChi, errDiaChi);
        }

        private void ClearSingleError(TextBox tb, TextBlock errorBlock)
        {
            if (errorBlock.Visibility == Visibility.Visible)
            {
                tb.BorderBrush = _defaultBorderBrush;
                errorBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearVisualErrors()
        {
            txtTenKH.BorderBrush = _defaultBorderBrush;
            txtSdt.BorderBrush = _defaultBorderBrush;
            txtEmail.BorderBrush = _defaultBorderBrush;
            txtDiaChi.BorderBrush = _defaultBorderBrush;

            errTenKH.Visibility = Visibility.Collapsed;
            errSdt.Visibility = Visibility.Collapsed;
            errEmail.Visibility = Visibility.Collapsed;
            errDiaChi.Visibility = Visibility.Collapsed;
        }

        private void ShowVisualError(TextBox textBox, TextBlock errorBlock, string message)
        {
            textBox.BorderBrush = _errorBorderBrush;
            errorBlock.Text = message;
            errorBlock.Visibility = Visibility.Visible;
        }

        private bool ValidateInput()
        {
            ClearVisualErrors();
            bool isValid = true;
            Control firstErrorControl = null;

            if (string.IsNullOrWhiteSpace(txtTenKH.Text))
            {
                ShowVisualError(txtTenKH, errTenKH, "Tên khách hàng không được để trống");
                isValid = false;
                if (firstErrorControl == null) firstErrorControl = txtTenKH;
            }

            string sdt = txtSdt.Text.Trim();
            if (string.IsNullOrWhiteSpace(sdt))
            {
                ShowVisualError(txtSdt, errSdt, "Vui lòng nhập số điện thoại");
                isValid = false;
                if (firstErrorControl == null) firstErrorControl = txtSdt;
            }
            else if (!Regex.IsMatch(sdt, @"^\d{10}$"))
            {
                ShowVisualError(txtSdt, errSdt, "SĐT phải đúng 10 chữ số");
                isValid = false;
                if (firstErrorControl == null) firstErrorControl = txtSdt;
            }

            string email = txtEmail.Text.Trim();
            if (!string.IsNullOrEmpty(email))
            {
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(email, emailPattern))
                {
                    ShowVisualError(txtEmail, errEmail, "Email sai định dạng");
                    isValid = false;
                    if (firstErrorControl == null) firstErrorControl = txtEmail;
                }
            }

            if (string.IsNullOrWhiteSpace(txtDiaChi.Text))
            {
                ShowVisualError(txtDiaChi, errDiaChi, "Vui lòng nhập địa chỉ");
                isValid = false;
                if (firstErrorControl == null) firstErrorControl = txtDiaChi;
            }

            if (!isValid && firstErrorControl != null)
            {
                firstErrorControl.Focus();
                if (firstErrorControl is TextBox tb) tb.SelectAll();
            }

            return isValid;
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // Lấy loại KH từ ComboBox
                    string loaiKH = (cbbLoaiKH.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Thường";

                    if (_isEditMode)
                    {
                        var dbKH = context.Khachhangs.Find(_currentKH.Makh);
                        if (dbKH != null)
                        {
                            dbKH.Tenkh = txtTenKH.Text.Trim();
                            dbKH.Sdt = txtSdt.Text.Trim();
                            dbKH.Email = txtEmail.Text.Trim();
                            dbKH.Diachi = txtDiaChi.Text.Trim();
                            dbKH.Loaikh = loaiKH;
                            // Doanh số thường được tính tự động từ Hóa đơn, không nên sửa thủ công ở đây

                            context.SaveChanges();
                            MessageBox.Show("Cập nhật thành công!", "Thông báo");
                        }
                    }
                    else
                    {
                        if (context.Khachhangs.Any(x => x.Makh == txtMaKH.Text))
                        {
                            GenerateNewKHCode();
                        }

                        var newKH = new Khachhang
                        {
                            Makh = txtMaKH.Text,
                            Tenkh = txtTenKH.Text.Trim(),
                            Sdt = txtSdt.Text.Trim(),
                            Email = txtEmail.Text.Trim(),
                            Diachi = txtDiaChi.Text.Trim(),
                            Loaikh = loaiKH,
                            Doanhso = 0 // Mặc định 0 khi mới tạo
                        };
                        context.Khachhangs.Add(newKH);
                        context.SaveChanges();
                        MessageBox.Show($"Đã thêm khách hàng: {newKH.Tenkh}", "Thành công");
                    }

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu dữ liệu: " + ex.Message);
            }
        }

        private void GenerateNewKHCode()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var allCodes = context.Khachhangs.Select(n => n.Makh).ToList();
                    int nextNumber = 1;
                    while (true)
                    {
                        string candidateCode = $"KH{nextNumber:D3}"; // Tạo mã dạng KH001, KH002...
                        if (!allCodes.Any(c => c.Equals(candidateCode, StringComparison.OrdinalIgnoreCase)))
                        {
                            txtMaKH.Text = candidateCode;
                            break;
                        }
                        nextNumber++;
                    }
                }
            }
            catch { txtMaKH.Text = "KH???"; }
        }

        private void LoadDataToForm()
        {
            if (_currentKH != null)
            {
                txtMaKH.Text = _currentKH.Makh;
                txtTenKH.Text = _currentKH.Tenkh;
                txtSdt.Text = _currentKH.Sdt;
                txtEmail.Text = _currentKH.Email;
                txtDiaChi.Text = _currentKH.Diachi;

                // Set ComboBox Loại KH
                foreach (ComboBoxItem item in cbbLoaiKH.Items)
                {
                    if (item.Content.ToString() == _currentKH.Loaikh)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }

                // Hiển thị doanh số (Read-only)
                txtDoanhSo.Text = $"{_currentKH.Doanhso:N0}";
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}