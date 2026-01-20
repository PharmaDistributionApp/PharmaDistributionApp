using PharmaDistributionApp.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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
            using (var context = new QuanlyphanphoiduocphamContext())
            {
                var sp = context.Sanphams.FirstOrDefault(x => x.Masp == _maSPSua);
                if (sp != null)
                {
                    txtMaSP.Text = sp.Masp;
                    txtTenSP.Text = sp.Tensp;
                    txtGiaBan.Text = sp.Giaban.ToString("N0");
                    cboDVT.SelectedItem = sp.Dvt;
                    cboNuocSX.SelectedItem = sp.Nuocsx;

                    cboLoaiThuoc.SelectedValue = sp.Maloai;
                    cboNhaCungCap.SelectedValue = sp.Nhacungcap;
                }
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
                    cboNhaCungCap.ItemsSource = context.Nhacungcaps
                                                       .Select(x => new { x.Mancc, TenNCC = x.Tenncc })
                                                       .ToList();
                    var listDVT = new List<string> { "Hộp", "Vỉ", "Lọ", "Chai", "Ống", "Viên" };
                    cboDVT.ItemsSource = listDVT;

                    var listNuoc = new List<string> { "Việt Nam", "Trung Quốc", "Mỹ", "Pháp", "Đức", "Ấn Độ", "Hàn Quốc", "Nhật Bản" };
                    cboNuocSX.ItemsSource = listNuoc;
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải danh mục: " + ex.Message); }
        }
        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (cboDVT.SelectedItem == null || cboNuocSX.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn đầy đủ Đơn vị tính và Nước sản xuất!");
                return;
            }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    if (_maSPSua == null) 
                    {
                        var spMoi = new Sanpham
                        {
                            Masp = txtMaSP.Text,
                            Tensp = txtTenSP.Text.Trim(),
                            Dvt = cboDVT.SelectedItem.ToString(),     
                            Nuocsx = cboNuocSX.SelectedItem.ToString(), 
                            Giaban = decimal.TryParse(txtGiaBan.Text, out decimal gia) ? gia : 0,
                            Maloai = cboLoaiThuoc.SelectedValue?.ToString(),
                            Nhacungcap = cboNhaCungCap.SelectedValue?.ToString(),
                            Ghichu = "Đang nhập"
                        };
                        context.Sanphams.Add(spMoi);
                    }
                    else // CẬP NHẬT
                    {
                        var spCu = context.Sanphams.FirstOrDefault(x => x.Masp == _maSPSua);
                        if (spCu != null)
                        {
                            spCu.Tensp = txtTenSP.Text.Trim();
                            spCu.Dvt = cboDVT.SelectedItem.ToString();
                            spCu.Nuocsx = cboNuocSX.SelectedItem.ToString();
                            spCu.Giaban = decimal.TryParse(txtGiaBan.Text, out decimal gia) ? gia : 0;
                            spCu.Maloai = cboLoaiThuoc.SelectedValue?.ToString();
                            spCu.Nhacungcap = cboNhaCungCap.SelectedValue?.ToString();
                        }
                    }
                    context.SaveChanges();
                    OnProductAdded?.Invoke();
                    this.Close();
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}