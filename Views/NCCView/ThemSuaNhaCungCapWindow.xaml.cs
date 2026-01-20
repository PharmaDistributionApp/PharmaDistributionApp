using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views.NCCView
{
    public partial class ThemSuaNhaCungCapWindow : Window
    {
        private Nhacungcap _currentNCC;
        private bool _isEditMode = false;

        private readonly Brush _errorBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
        private readonly Brush _defaultBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#89000000"));

        public ThemSuaNhaCungCapWindow()
        {
            InitializeComponent();
            SetupWindowLogic();
            _isEditMode = false;
            lblTitle.Text = "THÊM NHÀ CUNG CẤP MỚI";
            GenerateNewNCCCode();
        }

        public ThemSuaNhaCungCapWindow(Nhacungcap ncc)
        {
            InitializeComponent();
            SetupWindowLogic();
            _isEditMode = true;
            _currentNCC = ncc;
            lblTitle.Text = "CẬP NHẬT THÔNG TIN";
            LoadDataToForm();
        }

        private void SetupWindowLogic()
        {
            // 1. Xử lý phím tắt
            this.KeyDown += (s, e) => {
                // Phím ESC để thoát
                if (e.Key == Key.Escape)
                {
                    this.Close();
                }
                // Phím ENTER để Lưu (Khôi phục phần này)
                else if (e.Key == Key.Enter)
                {
                    // Gọi hàm lưu, truyền null vì ta không cần đối tượng sender
                    BtnLuu_Click(null, null);
                }
            };

            // 2. Focus vào ô nhập tên ngay khi mở cửa sổ
            this.Loaded += (s, e) => txtTenNCC.Focus();

            // 3. Tự động xóa thông báo lỗi khi người dùng bắt đầu nhập lại
            txtTenNCC.TextChanged += (s, e) => ClearSingleError(txtTenNCC, errTenNCC);
            txtSdt.TextChanged += (s, e) => ClearSingleError(txtSdt, errSdt);
            txtEmail.TextChanged += (s, e) => ClearSingleError(txtEmail, errEmail);
            txtDiaChi.TextChanged += (s, e) => ClearSingleError(txtDiaChi, errDiaChi);
        }

        // --- HÀM MỚI: Xóa lỗi cho 1 ô cụ thể ---
        private void ClearSingleError(TextBox tb, TextBlock errorBlock)
        {
            // Chỉ reset nếu hiện tại nó đang bị đỏ (để tránh xử lý thừa)
            if (errorBlock.Visibility == Visibility.Visible)
            {
                tb.BorderBrush = _defaultBorderBrush; // Trả lại màu viền xám
                errorBlock.Visibility = Visibility.Collapsed; // Ẩn dòng chữ lỗi
            }
        }

        // --- Các hàm Validate cũ (Giữ nguyên logic) ---
        private void ClearVisualErrors()
        {
            txtTenNCC.BorderBrush = _defaultBorderBrush;
            txtSdt.BorderBrush = _defaultBorderBrush;
            txtEmail.BorderBrush = _defaultBorderBrush;
            txtDiaChi.BorderBrush = _defaultBorderBrush;

            errTenNCC.Visibility = Visibility.Collapsed;
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
            // Lưu ý: Không gọi ClearVisualErrors() ở đầu nữa 
            // vì ta muốn giữ lại các lỗi khác nếu người dùng chỉ sửa 1 ô.
            // Tuy nhiên, để đảm bảo logic sạch sẽ mỗi khi bấm Lưu, ta có thể gọi ClearVisualErrors() 
            // HOẶC để nguyên logic hiển thị lỗi đè lên. 

            // Ở đây tôi chọn cách: Gọi ClearVisualErrors() để validate lại từ đầu
            ClearVisualErrors();

            bool isValid = true;
            Control firstErrorControl = null;

            // Kiểm tra Tên
            if (string.IsNullOrWhiteSpace(txtTenNCC.Text))
            {
                ShowVisualError(txtTenNCC, errTenNCC, "Tên nhà cung cấp không được để trống");
                isValid = false;
                if (firstErrorControl == null) firstErrorControl = txtTenNCC;
            }

            // Kiểm tra SĐT
            string sdt = txtSdt.Text.Trim();
            if (string.IsNullOrWhiteSpace(sdt))
            {
                ShowVisualError(txtSdt, errSdt, "Vui lòng nhập số điện thoại");
                isValid = false;
                if (firstErrorControl == null) firstErrorControl = txtSdt;
            }
            else if (!Regex.IsMatch(sdt, @"^\d{10}$"))
            {
                ShowVisualError(txtSdt, errSdt, "SĐT không hợp lệ (Phải đúng 10 chữ số)");
                isValid = false;
                if (firstErrorControl == null) firstErrorControl = txtSdt;
            }

            // Kiểm tra Email
            string email = txtEmail.Text.Trim();
            if (!string.IsNullOrEmpty(email))
            {
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(email, emailPattern))
                {
                    ShowVisualError(txtEmail, errEmail, "Email sai định dạng (vd: abc@mail.com)");
                    isValid = false;
                    if (firstErrorControl == null) firstErrorControl = txtEmail;
                }
            }

            // Kiểm tra Địa chỉ
            if (string.IsNullOrWhiteSpace(txtDiaChi.Text))
            {
                ShowVisualError(txtDiaChi, errDiaChi, "Vui lòng nhập địa chỉ trụ sở");
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

        // --- CÁC HÀM XỬ LÝ KHÁC (GIỮ NGUYÊN) ---
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    if (_isEditMode)
                    {
                        var dbNCC = context.Nhacungcaps.Find(_currentNCC.Mancc);
                        if (dbNCC != null)
                        {
                            dbNCC.Tenncc = txtTenNCC.Text.Trim();
                            dbNCC.Sdt = txtSdt.Text.Trim();
                            dbNCC.Email = txtEmail.Text.Trim();
                            dbNCC.Diachi = txtDiaChi.Text.Trim();
                            context.SaveChanges();
                            MessageBox.Show("Cập nhật thành công!", "Thông báo");
                        }
                    }
                    else
                    {
                        if (context.Nhacungcaps.Any(x => x.Mancc == txtMaNCC.Text))
                        {
                            GenerateNewNCCCode();
                        }

                        var newNCC = new Nhacungcap
                        {
                            Mancc = txtMaNCC.Text,
                            Tenncc = txtTenNCC.Text.Trim(),
                            Sdt = txtSdt.Text.Trim(),
                            Email = txtEmail.Text.Trim(),
                            Diachi = txtDiaChi.Text.Trim()
                        };
                        context.Nhacungcaps.Add(newNCC);
                        context.SaveChanges();
                        MessageBox.Show($"Đã thêm: {newNCC.Tenncc}", "Thành công");
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

        private void GenerateNewNCCCode()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var allCodes = context.Nhacungcaps.Select(n => n.Mancc).ToList();
                    int nextNumber = 1;
                    while (true)
                    {
                        string candidateCode = $"NCC{nextNumber:D3}";
                        if (!allCodes.Any(c => c.Equals(candidateCode, StringComparison.OrdinalIgnoreCase)))
                        {
                            txtMaNCC.Text = candidateCode;
                            break;
                        }
                        nextNumber++;
                    }
                }
            }
            catch { txtMaNCC.Text = "NCC???"; }
        }

        private void LoadDataToForm()
        {
            if (_currentNCC != null)
            {
                txtMaNCC.Text = _currentNCC.Mancc;
                txtTenNCC.Text = _currentNCC.Tenncc;
                txtSdt.Text = _currentNCC.Sdt;
                txtEmail.Text = _currentNCC.Email;
                txtDiaChi.Text = _currentNCC.Diachi;
            }
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}