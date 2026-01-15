using System;
using System.Linq;
using System.Windows;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class ThemSanPhamWindow : Window
    {
        public event Action OnProductAdded;

        public ThemSanPhamWindow()
        {
            InitializeComponent();
            LoadComboBoxData();

            // [SỬA 1] Gọi hàm tạo mã ngay khi khởi tạo để nó hiện lên giao diện
            TaoMaTuDong();
        }

        // --- HÀM TẠO MÃ TỰ ĐỘNG ---
        private void TaoMaTuDong()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // Lấy danh sách mã hiện có
                    var danhSachMa = context.Sanphams.Select(x => x.Masp).ToList();
                    int maxNumber = 0;

                    foreach (var ma in danhSachMa)
                    {
                        // Logic: Cắt bỏ "SP_" lấy số đuôi
                        if (ma.StartsWith("SP_") && ma.Length > 3)
                        {
                            string phanSo = ma.Substring(3); // Bỏ 3 ký tự đầu
                            if (int.TryParse(phanSo, out int number))
                            {
                                if (number > maxNumber) maxNumber = number;
                            }
                        }
                    }

                    // Tăng lên 1 đơn vị
                    int nextNumber = maxNumber + 1;

                    // Gán vào Textbox để hiển thị cho người dùng thấy ngay
                    // "D3" nghĩa là số 5 sẽ thành 005
                    txtMaSP.Text = "SP_" + nextNumber.ToString("D3");
                }
            }
            catch (Exception)
            {
                // Nếu lỗi kết nối DB hoặc bảng trống, mặc định bắt đầu từ 001
                txtMaSP.Text = "SP_001";
            }
        }

        private void LoadComboBoxData()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    cboLoaiThuoc.ItemsSource = context.Loaisps.ToList();
                    // Lấy tên NCC đổ vào combobox
                    cboNhaCungCap.ItemsSource = context.Nhacungcaps
                                                       .Select(x => new { x.Mancc, TenNCC = x.Tenncc })
                                                       .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        // --- HÀM LƯU SẢN PHẨM (ĐÃ SỬA LỖI VALIDATE) ---
        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            // [SỬA 2 - QUAN TRỌNG] 
            // Chỉ kiểm tra Tên sản phẩm, KHÔNG kiểm tra txtMaSP nữa vì nó đã tự động điền
            if (string.IsNullOrWhiteSpace(txtTenSP.Text))
            {
                MessageBox.Show("Vui lòng nhập Tên sản phẩm!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTenSP.Focus();
                return;
            }

            if (cboNhaCungCap.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn Nhà cung cấp!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // Lấy mã từ ô Textbox (Lúc này đã có chữ SP_xxx rồi)
                    string finalMaSP = txtMaSP.Text;

                    // Check trùng lần cuối cho chắc chắn (đề phòng 2 người cùng mở form)
                    if (context.Sanphams.Any(x => x.Masp == finalMaSP))
                    {
                        // Nếu trùng thì tự động +1 tiếp
                        TaoMaTuDong();
                        finalMaSP = txtMaSP.Text;
                    }

                    var spMoi = new Sanpham()
                    {
                        Masp = finalMaSP,
                        Tensp = txtTenSP.Text.Trim(),
                        Dvt = txtDVT.Text.Trim(),
                        // Chuyển đổi giá bán an toàn
                        Giaban = decimal.TryParse(txtGiaBan.Text, out decimal gia) ? gia : 0,
                        Nuocsx = txtNuocSX.Text.Trim(),
                        Hoatchat = txtHoatChat.Text.Trim(),
                        Maloai = cboLoaiThuoc.SelectedValue?.ToString(),
                        Nhacungcap = cboNhaCungCap.SelectedValue?.ToString(),
                        Ghichu = "Đang nhập"
                    };

                    context.Sanphams.Add(spMoi);
                    context.SaveChanges();

                    MessageBox.Show($"Thêm thành công! Mã: {finalMaSP}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    OnProductAdded?.Invoke();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}