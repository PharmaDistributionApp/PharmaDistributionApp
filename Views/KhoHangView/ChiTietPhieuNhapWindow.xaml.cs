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
                "Xác nhận xử lý phiếu nhập này?\n\n- YES: Phê duyệt (Nhập kho)\n- NO: Từ chối (Hủy phiếu - Chỉ xóa tồn kho)\n- CANCEL: Thoát",
                "Xử lý phiếu", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

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
                        if (phieu.Trangthai == "Đã duyệt" || phieu.Trangthai == "Đã hủy")
                        {
                            MessageBox.Show($"Phiếu này đã xử lý rồi ({phieu.Trangthai})!", "Cảnh báo");
                            return;
                        }

                        var listChiTiet = context.Cthdnhaps.Where(ct => ct.Sohdnhap == phieu.Sohdnhap).ToList();

                        // ---------------------------------------------------------
                        // TRƯỜNG HỢP 1: DUYỆT (YES) - Logic cũ (Không đổi)
                        // ---------------------------------------------------------
                        if (trangThaiMoi == "Đã duyệt")
                        {
                            if (listChiTiet.Count == 0) { MessageBox.Show("Phiếu rỗng!"); return; }

                            foreach (var item in listChiTiet)
                            {
                                // 1. Tạo/Update Lô (Giữ nguyên)
                                var loHangCheck = context.Lohangs.FirstOrDefault(l => l.Malo == item.Malo);
                                var nsxDefault = DateOnly.FromDateTime(DateTime.Now);
                                var hsdDefault = DateOnly.FromDateTime(DateTime.Now.AddYears(2));

                                if (loHangCheck == null)
                                {
                                    context.Lohangs.Add(new Lohang { Malo = item.Malo, Masp = item.Masp, Nhacungcap = phieu.Sohdnhap, Nsx = nsxDefault, Hsd = hsdDefault });
                                }
                                else
                                {
                                    if (loHangCheck.Nsx == null) loHangCheck.Nsx = nsxDefault;
                                    context.Entry(loHangCheck).State = EntityState.Modified;
                                }

                                // 2. Cộng kho (Giữ nguyên)
                                var tonKho = context.Tonkhos.FirstOrDefault(t => t.Masp == item.Masp && t.Malo == item.Malo && t.Makho == phieu.Makho);
                                if (tonKho != null)
                                {
                                    tonKho.Soluongton += item.Soluong;
                                    context.Entry(tonKho).State = EntityState.Modified;
                                }
                                else
                                {
                                    context.Tonkhos.Add(new Tonkho { Masp = item.Masp, Malo = item.Malo, Makho = phieu.Makho, Soluongton = item.Soluong });
                                }
                            }
                        }
                        else if (trangThaiMoi == "Đã hủy")
                        {
                            var loCanXet = listChiTiet.Select(x => x.Malo).Distinct().ToList();

                            foreach (var maLo in loCanXet)
                            {
                                if (string.IsNullOrEmpty(maLo)) continue;
                                var tonKho = context.Tonkhos.FirstOrDefault(t => t.Malo == maLo);
                                bool laTonKhoRac = (tonKho != null) && (tonKho.Soluongton == 0);

                                if (laTonKhoRac)
                                {
                                    context.Tonkhos.Remove(tonKho);
                                }
                            }
                        }

                        // Cập nhật trạng thái phiếu
                        phieu.Trangthai = trangThaiMoi;
                        context.Entry(phieu).State = EntityState.Modified;

                        // Lưu tất cả
                        context.SaveChanges();

                        MessageBox.Show($"Đã {trangThaiMoi} thành công!", "Thông báo");
                        LoadData(_maPN);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}