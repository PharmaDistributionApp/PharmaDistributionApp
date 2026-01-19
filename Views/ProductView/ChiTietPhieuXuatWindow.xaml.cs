using Microsoft.EntityFrameworkCore;
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
                                    ct.Malo,
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
                        // Kiểm tra trạng thái hiện tại (tránh duyệt lại phiếu đã duyệt)
                        if (px.Trangthai == "Đã duyệt" || px.Trangthai == "Đã hủy")
                        {
                            MessageBox.Show("Phiếu này đã được xử lý rồi!", "Cảnh báo");
                            return;
                        }

                        // Lấy danh sách chi tiết
                        var listChiTiet = context.Cthdxuats.Where(ct => ct.Sohdxuat == px.Sohdxuat).ToList();

                        // [FIX 1] Kiểm tra xem có chi tiết không. Nếu rỗng thì không trừ kho được.
                        if (listChiTiet.Count == 0 && trangThaiMoi == "Đã duyệt")
                        {
                            MessageBox.Show("Lỗi: Không tìm thấy chi tiết sản phẩm của hóa đơn này!\nVui lòng kiểm tra lại dữ liệu Hóa đơn.", "Lỗi dữ liệu");
                            return;
                        }

                        // --- LOGIC CẬP NHẬT TỒN KHO ---
                        if (trangThaiMoi == "Đã duyệt")
                        {
                            // BƯỚC 1: KIỂM TRA ĐỦ HÀNG KHÔNG? (Check All First)
                            foreach (var item in listChiTiet)
                            {
                                var tonKho = context.Tonkhos.FirstOrDefault(t =>
                                    t.Masp == item.Masp &&
                                    t.Malo == item.Malo && // Phải khớp Lô
                                    t.Makho == px.Makho);  // Phải khớp Kho

                                if (tonKho == null || tonKho.Soluongton < item.Soluong)
                                {
                                    MessageBox.Show($"Lỗi: Không đủ hàng để xuất!\n" +
                                                    $"- SP: {item.Masp} (Lô: {item.Malo})\n" +
                                                    $"- Kho: {px.Makho}\n" +
                                                    $"- Tồn: {tonKho?.Soluongton ?? 0} | Cần: {item.Soluong}",
                                                    "Không thể duyệt", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return; // Dừng ngay lập tức
                                }
                            }

                            // BƯỚC 2: TRỪ KHO (Execute)
                            foreach (var item in listChiTiet)
                            {
                                // Tìm lại đúng object đó để update
                                var tonKho = context.Tonkhos.FirstOrDefault(t =>
                                    t.Masp == item.Masp &&
                                    t.Malo == item.Malo &&
                                    t.Makho == px.Makho);

                                if (tonKho != null)
                                {
                                    tonKho.Soluongton -= item.Soluong;

                                    // [FIX 2] Ép buộc EF Core đánh dấu là đã sửa đổi (Modified)
                                    context.Entry(tonKho).State = EntityState.Modified;
                                }
                            }
                        }

                        // Cập nhật trạng thái phiếu
                        px.Trangthai = trangThaiMoi;
                        context.Entry(px).State = EntityState.Modified; // Đảm bảo trạng thái được lưu

                        context.SaveChanges();

                        MessageBox.Show($"Đã {trangThaiMoi} phiếu và cập nhật kho thành công!", "Thông báo");

                        // Load lại giao diện chi tiết để thấy trạng thái mới
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