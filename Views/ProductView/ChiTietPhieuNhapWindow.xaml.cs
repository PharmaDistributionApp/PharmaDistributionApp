using Microsoft.EntityFrameworkCore;
using PharmaDistributionApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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
                        ct.MaloNavigation.Sohieu,
                        Hsd = ct.MaloNavigation.Hsd.ToString(),
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
                    // --- SỬA LỖI 2: DÙNG BIẾN _maPN THAY VÌ CẮT CHUỖI ---
                    // Entity Framework sẽ hiểu _maPN là giá trị string, không bị lỗi Expression tree nữa
                    var phieu = context.Phieunhaps.FirstOrDefault(p => p.Mapn == _maPN);

                    if (phieu != null)
                    {
                        // --- LOGIC CẬP NHẬT TỒN KHO ---
                        if (trangThaiMoi == "Đã duyệt")
                        {
                            var listChiTiet = context.Cthdnhaps.Where(ct => ct.Sohdnhap == phieu.Sohdnhap).ToList();

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