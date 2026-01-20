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
    public partial class ChiTietPhieuNhapWindow : Window
    {
        // --- 1. KHAI BÁO BIẾN TOÀN CỤC Ở ĐÂY (SỬA LỖI 1) ---
        private string _maPN;

        public ChiTietPhieuNhapWindow(string maPhieuNhap)
        {
            InitializeComponent();

            // 2. Lưu giá trị vào biến
            this._maPN = maPhieuNhap;

            // Tải dữ liệu lên
            LoadData(_maPN);
        }

        private void LoadData(string maPN)
        {
            using (var context = new QuanlyphanphoiduocphamContext())
            {
                // Lấy thông tin phiếu nhập
                var phieu = context.Phieunhaps.FirstOrDefault(p => p.Mapn == maPN);
                if (phieu == null) return;

                txtTieuDe.Text = "PHIẾU NHẬP KHO: " + phieu.Mapn;
                txtNgayLap.Text = phieu.Ngaynhap;
                txtTrangThai.Text = phieu.Trangthai;
                txtSoHD.Text = phieu.Sohdnhap;

                // Lấy tên Kho
                var kho = context.Khos.FirstOrDefault(k => k.Makho == phieu.Makho);
                txtKho.Text = kho != null ? kho.Tenkho : phieu.Makho;

                // Lấy thông tin từ Hóa đơn gốc
                var hd = context.Hoadonnhaps.FirstOrDefault(h => h.Sohdnhap == phieu.Sohdnhap);
                if (hd != null)
                {
                    var ncc = context.Nhacungcaps.FirstOrDefault(n => n.Mancc == hd.Mancc);
                    txtNCC.Text = ncc != null ? ncc.Tenncc : hd.Mancc;
                }

                // Lấy chi tiết sản phẩm
                var listChiTiet = context.Cthdnhaps
                 .Include(ct => ct.MaspNavigation)
                 .Include(ct => ct.MaloNavigation)
                 .Where(ct => ct.Sohdnhap == phieu.Sohdnhap)
                 .Select(ct => new
                 {
                     ct.Masp,
                     ct.MaspNavigation.Tensp,
                     ct.MaspNavigation.Dvt,

                     // [QUAN TRỌNG] Thêm dòng này để lấy Mã Lô
                     ct.Malo,

                     // Xử lý null cho HSD để tránh lỗi
                     Hsd = ct.MaloNavigation.Hsd.HasValue ? ct.MaloNavigation.Hsd.Value.ToString("dd/MM/yyyy") : "---",

                     ct.Soluong,
                     ct.Dongianhap,
                     ct.Thanhtien
                 })
                 .ToList();

                // Logic ẩn hiện nút duyệt
                bool coQuyen = false;
                if (UserSession.CurrentUser != null)
                {
                    string chucVuHienTai = UserSession.CurrentUser.Chucvu;
                    string[] cacSep = { "Admin", "Giám đốc", "Quản lý kho" };
                    coQuyen = cacSep.Contains(chucVuHienTai);
                }

                bool dangChoDuyet = phieu.Trangthai == "Chờ duyệt";

                if (coQuyen && dangChoDuyet)
                    btnPheDuyet.Visibility = Visibility.Visible;
                else
                    btnPheDuyet.Visibility = Visibility.Collapsed;

                dgvChiTiet.ItemsSource = listChiTiet;
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dgvChiTiet.UnselectAll();
            Keyboard.ClearFocus();
        }

        private void BtnPheDuyet_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn PHÊ DUYỆT phiếu nhập này?\n(Kho sẽ được CỘNG thêm số lượng)",
                "Xác nhận", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            string trangThaiMoi = "";
            if (result == MessageBoxResult.Yes) trangThaiMoi = "Đã duyệt";
            else if (result == MessageBoxResult.No) trangThaiMoi = "Đã hủy";
            else return;

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var phieu = context.Phieunhaps.FirstOrDefault(p => p.Mapn == _maPN);

                    if (phieu != null)
                    {
                        // [FIX 1] Kiểm tra xem phiếu đã xử lý chưa để tránh cộng kho 2 lần
                        if (phieu.Trangthai == "Đã duyệt" || phieu.Trangthai == "Đã hủy")
                        {
                            MessageBox.Show("Phiếu này đã được xử lý rồi!", "Cảnh báo");
                            return;
                        }

                        // [FIX 2] Kiểm tra chi tiết rỗng
                        var listChiTiet = context.Cthdnhaps.Where(ct => ct.Sohdnhap == phieu.Sohdnhap).ToList();
                        if (listChiTiet.Count == 0 && trangThaiMoi == "Đã duyệt")
                        {
                            MessageBox.Show("Lỗi: Không tìm thấy chi tiết sản phẩm để nhập kho!", "Lỗi dữ liệu");
                            return;
                        }

                        // --- LOGIC CẬP NHẬT TỒN KHO ---
                        if (trangThaiMoi == "Đã duyệt")
                        {
                            foreach (var item in listChiTiet)
                            {
                                // Tìm hàng trong bảng Tonkho
                                var tonKho = context.Tonkhos.FirstOrDefault(t =>
                                    t.Masp == item.Masp &&
                                    t.Malo == item.Malo &&
                                    t.Makho == phieu.Makho);

                                if (tonKho != null)
                                {
                                    // Đã có -> Cộng thêm
                                    tonKho.Soluongton += item.Soluong;

                                    // [QUAN TRỌNG] Báo cho EF biết dòng này đã sửa
                                    context.Entry(tonKho).State = EntityState.Modified;
                                }
                                else
                                {
                                    // Chưa có -> Tạo mới
                                    var moi = new Tonkho
                                    {
                                        Masp = item.Masp,
                                        Malo = item.Malo,
                                        Makho = phieu.Makho,
                                        Soluongton = item.Soluong
                                    };
                                    context.Tonkhos.Add(moi);
                                }
                            }
                        }
                        // ------------------------------

                        phieu.Trangthai = trangThaiMoi;
                        context.Entry(phieu).State = EntityState.Modified; // Đảm bảo lưu trạng thái phiếu

                        context.SaveChanges();

                        MessageBox.Show($"Đã cập nhật: {trangThaiMoi} và nhập kho thành công!", "Thông báo");
                        LoadData(_maPN);
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