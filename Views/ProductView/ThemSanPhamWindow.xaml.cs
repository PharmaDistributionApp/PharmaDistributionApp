using System;
using System.Linq;
using System.Windows;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class ThemSanPhamWindow : Window
    {
        public event Action OnProductAdded;
        private string _maSPSua = null; // Biến này: null = Thêm mới, Có giá trị = Đang sửa

        // -----------------------------------------------------------
        // 1. CONSTRUCTOR MẶC ĐỊNH (Dùng cho THÊM MỚI)
        // -----------------------------------------------------------
        public ThemSanPhamWindow()
        {
            InitializeComponent();
            LoadComboBoxData();

            // Logic cũ của bạn: Tự động tạo mã khi thêm mới
            TaoMaTuDong();

            _maSPSua = null;
        }

        // -----------------------------------------------------------
        // 2. CONSTRUCTOR MỚI (Dùng cho CHỈNH SỬA)
        // -----------------------------------------------------------
        public ThemSanPhamWindow(string maSP)
        {
            InitializeComponent();
            LoadComboBoxData();

            _maSPSua = maSP; // Lưu lại mã đang sửa

            // Ở chế độ sửa:
            // 1. Không gọi TaoMaTuDong()
            // 2. Load dữ liệu cũ lên form
            LoadDataDeSua();

            txtTitle.Text = "CẬP NHẬT SẢN PHẨM";
            btnLuu.Content = "Lưu thay đổi";
        }

        // --- HÀM LOAD DỮ LIỆU CŨ KHI SỬA ---
        private void LoadDataDeSua()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var sp = context.Sanphams.FirstOrDefault(x => x.Masp == _maSPSua);
                    if (sp != null)
                    {
                        txtMaSP.Text = sp.Masp;
                        txtMaSP.IsReadOnly = true;

                        txtTenSP.Text = sp.Tensp;
                        txtDVT.Text = sp.Dvt;

                        // Xử lý hiển thị giá
                        txtGiaBan.Text = sp.Giaban.ToString("N0");

                        txtNuocSX.Text = sp.Nuocsx;

                        cboLoaiThuoc.SelectedValue = sp.Maloai;
                        cboNhaCungCap.SelectedValue = sp.Nhacungcap;

                        // Đã xóa phần gán txtSoLuong
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu sửa: " + ex.Message);
            }
        }

        private void TaoMaTuDong()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var danhSachMa = context.Sanphams.Select(x => x.Masp).ToList();
                    int maxNumber = 0;

                    foreach (var ma in danhSachMa)
                    {
                        if (ma != null && ma.StartsWith("SP_") && ma.Length > 3)
                        {
                            string phanSo = ma.Substring(3);
                            if (int.TryParse(phanSo, out int number))
                            {
                                if (number > maxNumber) maxNumber = number;
                            }
                        }
                    }
                    int nextNumber = maxNumber + 1;
                    txtMaSP.Text = "SP_" + nextNumber.ToString("D3");
                }
            }
            catch (Exception)
            {
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

                    // Logic cũ của bạn
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

        // --- HÀM LƯU (XỬ LÝ CẢ THÊM VÀ SỬA) ---
        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
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
                    // =========================================================
                    // TRƯỜNG HỢP 1: THÊM MỚI (Logic cũ của bạn)
                    // =========================================================
                    if (_maSPSua == null)
                    {
                        string finalMaSP = txtMaSP.Text;

                        // Check trùng và tự +1 nếu trùng (chỉ dùng khi thêm mới)
                        if (context.Sanphams.Any(x => x.Masp == finalMaSP))
                        {
                            TaoMaTuDong();
                            finalMaSP = txtMaSP.Text;
                        }

                        var spMoi = new Sanpham
                        {
                            Masp = finalMaSP,
                            Tensp = txtTenSP.Text.Trim(),
                            Dvt = txtDVT.Text.Trim(),
                            Giaban = decimal.TryParse(txtGiaBan.Text, out decimal gia) ? gia : 0,
                            Nuocsx = txtNuocSX.Text.Trim(),
                            Maloai = cboLoaiThuoc.SelectedValue?.ToString(),
                            Nhacungcap = cboNhaCungCap.SelectedValue?.ToString(), // Lưu ý: Xem lại DB lưu Mã hay Tên
                            Ghichu = "Đang nhập",
                        };

                        context.Sanphams.Add(spMoi);
                        context.SaveChanges();

                        MessageBox.Show($"Thêm thành công! Mã: {finalMaSP}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    // =========================================================
                    // TRƯỜNG HỢP 2: CẬP NHẬT (Logic Mới)
                    // =========================================================
                    else
                    {
                        // Tìm sản phẩm cũ theo mã
                        var spCu = context.Sanphams.FirstOrDefault(x => x.Masp == _maSPSua);
                        if (spCu != null)
                        {
                            // Cập nhật các trường thông tin
                            spCu.Tensp = txtTenSP.Text.Trim();
                            spCu.Dvt = txtDVT.Text.Trim();
                            spCu.Giaban = decimal.TryParse(txtGiaBan.Text, out decimal gia) ? gia : 0;
                            spCu.Nuocsx = txtNuocSX.Text.Trim();
                            spCu.Maloai = cboLoaiThuoc.SelectedValue?.ToString();
                            spCu.Nhacungcap = cboNhaCungCap.SelectedValue?.ToString();

                            // Không sửa Masp, Ghichu, Trangthai nếu không cần thiết

                            context.SaveChanges();
                            MessageBox.Show("Cập nhật thông tin thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    OnProductAdded?.Invoke(); // Gọi sự kiện reload dữ liệu ở màn hình chính
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