using Microsoft.EntityFrameworkCore;
using PharmaDistributionApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PharmaDistributionApp.Services;

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
                        if (px.Trangthai == "Đã duyệt" || px.Trangthai == "Đã hủy")
                        {
                            MessageBox.Show("Phiếu này đã được xử lý rồi!", "Cảnh báo");
                            return;
                        }

                        // Lấy danh sách chi tiết (Sản phẩm cần xuất)
                        var listChiTiet = context.Cthdxuats.Where(ct => ct.Sohdxuat == px.Sohdxuat).ToList();

                        if (listChiTiet.Count == 0 && trangThaiMoi == "Đã duyệt")
                        {
                            MessageBox.Show("Lỗi: Không tìm thấy chi tiết sản phẩm!", "Lỗi dữ liệu");
                            return;
                        }

                        // --- TRƯỜNG HỢP 1: DUYỆT (TRỪ KHO) ---
                        if (trangThaiMoi == "Đã duyệt")
                        {
                            // Bước A: Kiểm tra đủ hàng không?
                            foreach (var item in listChiTiet)
                            {
                                var tonKho = context.Tonkhos.FirstOrDefault(t =>
                                    t.Masp == item.Masp && t.Malo == item.Malo && t.Makho == px.Makho);

                                if (tonKho == null || tonKho.Soluongton < item.Soluong)
                                {
                                    MessageBox.Show($"Lỗi: Không đủ hàng!\nSP: {item.Masp} - Lô: {item.Malo}\nTồn: {tonKho?.Soluongton ?? 0} < Cần: {item.Soluong}", "Lỗi");
                                    return; // Dừng ngay
                                }
                            }

                            // Bước B: Trừ kho thật
                            foreach (var item in listChiTiet)
                            {
                                var tonKho = context.Tonkhos.FirstOrDefault(t =>
                                    t.Masp == item.Masp && t.Malo == item.Malo && t.Makho == px.Makho);

                                if (tonKho != null)
                                {
                                    tonKho.Soluongton -= item.Soluong;

                                    // [QUAN TRỌNG] Nếu hết hàng (sl = 0) -> Xóa dòng tồn kho này luôn cho sạch
                                    // Nếu bạn muốn giữ dòng tồn = 0 thì comment đoạn if này lại.
                                    if (tonKho.Soluongton == 0)
                                    {
                                        context.Tonkhos.Remove(tonKho);
                                    }
                                    else
                                    {
                                        context.Entry(tonKho).State = EntityState.Modified;
                                    }
                                }
                            }
                        }

                        // --- TRƯỜNG HỢP 2: HỦY ---
                        else if (trangThaiMoi == "Đã hủy")
                        {
                            // Nếu phiếu xuất chỉ là nháp (chưa trừ kho) thì Hủy đơn giản là đổi trạng thái.
                            // Không cần làm gì thêm ở đây.
                        }

                        // Cập nhật trạng thái phiếu
                        px.Trangthai = trangThaiMoi;
                        context.Entry(px).State = EntityState.Modified;

                        context.SaveChanges();

                        MessageBox.Show($"Đã {trangThaiMoi} phiếu thành công!", "Thông báo");
                        LoadChiTiet();
                    }
                }
            }
            catch (Exception ex)
            {
                // Hiển thị lỗi chi tiết (Inner Exception) để dễ debug nếu dính khóa ngoại
                string msg = ex.Message;
                if (ex.InnerException != null) msg += "\nChi tiết: " + ex.InnerException.Message;
                MessageBox.Show("Lỗi cập nhật: " + msg);
            }
        }
        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}