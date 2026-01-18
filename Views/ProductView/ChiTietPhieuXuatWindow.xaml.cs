using PharmaDistributionApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class ChiTietPhieuXuatWindow : Window
    {
        private string _mapx;

        public ChiTietPhieuXuatWindow(string mapx)
        {
            InitializeComponent();
            this._mapx = mapx;
            LoadChiTiet();
        }

        private void LoadChiTiet()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // 1. Lấy thông tin phiếu xuất
                    var px = context.Phieuxuats.FirstOrDefault(p => p.Mapx == _mapx);
                    if (px == null) return;

                    txtTieuDe.Text = "PHIẾU XUẤT KHO: " + px.Mapx;
                    txtNgayLap.Text = px.Ngayxuat;
                    txtTrangThai.Text = px.Trangthai;
                    txtSoHD.Text = px.Sohdxuat;

                    // Lấy tên kho
                    var kho = context.Khos.FirstOrDefault(k => k.Makho == px.Makho);
                    txtKho.Text = kho != null ? kho.Tenkho : px.Makho;

                    // Lấy tên nhân viên
                    var nv = context.Nhanviens.FirstOrDefault(n => n.Manv == px.Manv);
                    txtNhanVien.Text = nv != null ? nv.Tennv : px.Manv;

                    // 2. Lấy thông tin Hóa đơn xuất để lấy tên Khách hàng
                    var hdx = context.Hoadonxuats.FirstOrDefault(h => h.Sohdxuat == px.Sohdxuat);
                    txtKhachHang.Text = hdx?.Makh ?? "---";

                    bool coQuyen = false;
                    if (UserSession.CurrentUser != null)
                    {
                        // Kiểm tra chức vụ (Role)
                        string chucVu = UserSession.CurrentUser.Chucvu; // Kiểm tra lại tên cột trong DB của bạn (Chucvu hay Vaitro?)
                        string[] cacSep = { "Admin", "Giám đốc", "Quản lý kho" };
                        coQuyen = cacSep.Contains(chucVu);
                    }

                    bool dangChoDuyet = px.Trangthai == "Chờ duyệt";

                    if (coQuyen && dangChoDuyet)
                    {
                        btnPheDuyet.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        btnPheDuyet.Visibility = Visibility.Collapsed;
                    }

                    // 3. Lấy chi tiết sản phẩm từ CTHDXUAT (vì Phiếu xuất liên kết qua Số HĐ)
                    var query = from ct in context.Cthdxuats
                                join sp in context.Sanphams on ct.Masp equals sp.Masp
                                join lh in context.Lohangs on ct.Malo equals lh.Malo into lhGroup
                                from lh in lhGroup.DefaultIfEmpty()
                                where ct.Sohdxuat == px.Sohdxuat
                                select new
                                {
                                    ct.Masp,
                                    sp.Tensp,
                                    sp.Dvt,
                                    Sohieu = lh != null ? lh.Sohieu : "---",
                                    Hsd = (lh != null && lh.Hsd.HasValue) ? lh.Hsd.Value.ToString("dd/MM/yyyy") : "---",
                                    ct.Soluong,
                                    ct.Dongiaban,
                                    ct.Thanhtien
                                };

                    dgvChiTiet.ItemsSource = query.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải chi tiết: " + ex.Message);
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Bỏ chọn dòng hiện tại trong DataGrid
            dgvChiTiet.UnselectAll();

            // Xóa focus vật lý khỏi DataGrid để mất các viền focus (nếu còn)
            Keyboard.ClearFocus();
        }

        private void BtnPheDuyet_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Xác nhận duyệt Phiếu Xuất này?\n(Kho sẽ bị TRỪ số lượng)",
                "Phê duyệt", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            string trangThaiMoi = "";
            if (result == MessageBoxResult.Yes) trangThaiMoi = "Đã duyệt";
            else if (result == MessageBoxResult.No) trangThaiMoi = "Đã hủy";
            else return;

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var px = context.Phieuxuats.FirstOrDefault(p => p.Mapx == _mapx);
                    if (px != null)
                    {
                        // --- LOGIC CẬP NHẬT TỒN KHO (BẢNG TONKHO) ---
                        if (trangThaiMoi == "Đã duyệt")
                        {
                            var listChiTiet = context.Cthdxuats.Where(ct => ct.Sohdxuat == px.Sohdxuat).ToList();

                            // BƯỚC 1: KIỂM TRA ĐỦ HÀNG KHÔNG?
                            foreach (var item in listChiTiet)
                            {
                                // Tìm trong bảng TONKHO
                                var tonKho = context.Tonkhos.FirstOrDefault(t =>
                                    t.Masp == item.Masp &&
                                    t.Malo == item.Malo &&
                                    t.Makho == px.Makho); // Lấy kho từ phiếu xuất

                                // Nếu không tìm thấy hoặc Số lượng tồn < Số lượng cần xuất
                                if (tonKho == null || tonKho.Soluongton < item.Soluong)
                                {
                                    MessageBox.Show($"Lỗi: Sản phẩm {item.Masp} (Lô {item.Malo}) không đủ hàng trong kho {px.Makho}!\n" +
                                                    $"Tồn kho: {tonKho?.Soluongton ?? 0} - Cần xuất: {item.Soluong}",
                                                    "Không thể duyệt", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return; // Dừng ngay
                                }
                            }

                            // BƯỚC 2: TRỪ KHO
                            foreach (var item in listChiTiet)
                            {
                                var tonKho = context.Tonkhos.FirstOrDefault(t =>
                                    t.Masp == item.Masp &&
                                    t.Malo == item.Malo &&
                                    t.Makho == px.Makho);

                                if (tonKho != null)
                                {
                                    tonKho.Soluongton -= item.Soluong;
                                }
                            }
                        }
                        // ------------------------------

                        px.Trangthai = trangThaiMoi;
                        context.SaveChanges();

                        MessageBox.Show($"Đã {trangThaiMoi} phiếu và trừ kho thành công!", "Thông báo");
                        LoadChiTiet();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật: " + ex.Message);
            }
        }
        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}