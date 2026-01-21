using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using PharmaDistributionApp.Models;
using MaterialDesignThemes.Wpf;
using PharmaDistributionApp.Services;
using Microsoft.Data.Sqlite;

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class TaoPhieuWindow : Window
    {
        private enum Mode { Nhap, Xuat }
        private Mode _currentMode = Mode.Nhap;
        private string _targetSoHD;

        public class ChiTietView
        {
            public string Masp { get; set; }
            public string Tensp { get; set; }
            public string Dvt { get; set; }
            public string Malo { get; set; }
            public string Hsd { get; set; }
            public int Soluong { get; set; }
            public decimal Dongia { get; set; } 
            public decimal Thanhtien { get; set; }
            public string SelectedMakho { get; set; }
        }

        private List<ChiTietView> _listChiTiet = new List<ChiTietView>();

        public TaoPhieuWindow(bool isXuat = false, string initSoHD = null)
        {
            InitializeComponent();
            _targetSoHD = initSoHD;
            dpNgayLap.SelectedDate = DateTime.Now;

            if (isXuat)
            {
                radXuat.IsChecked = true;
                _currentMode = Mode.Xuat;
            }
            else
            {
                radNhap.IsChecked = true;
                _currentMode = Mode.Nhap;
            }

            UpdateUIMode();
            LoadInitData(); 

            if (!string.IsNullOrEmpty(initSoHD))
            {
                cboHoaDon.SelectedValue = initSoHD;
            }
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!dgvChiTiet.IsMouseOver) dgvChiTiet.UnselectAll();
        }

        private void Mode_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = radNhap.IsChecked == true ? Mode.Nhap : Mode.Xuat;
            UpdateUIMode();

            cboHoaDon.ItemsSource = null;
            dgvChiTiet.ItemsSource = null;
            txtKhachHang.Text = "---"; txtNgayHD.Text = "---"; txtTongTienHD.Text = "0 đ";
            _listChiTiet.Clear();

            LoadInitData();
        }

        private void UpdateUIMode()
        {
            var color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C70BA"));


            if (UserSession.CurrentUser != null)
                txtNguoiLap.Text = UserSession.CurrentUser.Tennv;
            else
                txtNguoiLap.Text = "Admin (Test)";

            if (_currentMode == Mode.Nhap)
            {
                txtTieuDe.Text = "Tạo phiếu nhập hàng";
                HintAssist.SetHint(cboHoaDon, "Chọn Hóa Đơn Nhập");

                lblKhachHang.Visibility = Visibility.Collapsed;
                txtKhachHang.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtTieuDe.Text = "Tạo phiếu xuất hàng";
                HintAssist.SetHint(cboHoaDon, "Chọn Hóa Đơn Xuất");
                lblKhachHang.Visibility = Visibility.Visible;
                txtKhachHang.Visibility = Visibility.Visible;
                lblKhachHang.Text = "Khách hàng:"; 
            }

            txtTieuDe.Foreground = color;
            btnLuu.Background = color;
        }

        private void LoadInitData()
        {
            using (var context = new QuanlyphanphoiduocphamContext())
            {
                var listKho = context.Khos.ToList();
                CollectionViewSource cvs = (CollectionViewSource)this.Resources["cvsKhos"];
                cvs.Source = listKho;
                string target = _targetSoHD ?? "";

                if (_currentMode == Mode.Nhap)
                {
                    var listDaCoPhieu = context.Phieunhaps
                        .Where(p => p.Trangthai != "Đã hủy")
                        .Select(p => p.Sohdnhap)
                        .ToList();

                    var list = context.Hoadonnhaps
                        .Where(h => !listDaCoPhieu.Contains(h.Sohdnhap) || h.Sohdnhap == target)
                        .Select(h => new { Ma = h.Sohdnhap, HienThi = h.Sohdnhap })
                        .ToList();

                    cboHoaDon.ItemsSource = list;
                }
                else 
                {
                    var listDaCoPhieu = context.Phieuxuats
                        .Where(p => p.Trangthai != "Đã hủy")
                        .Select(p => p.Sohdxuat)
                        .ToList();

                    var list = context.Hoadonxuats
                        .Where(h => !listDaCoPhieu.Contains(h.Sohdxuat) || h.Sohdxuat == target)
                        .Select(h => new { Ma = h.Sohdxuat, HienThi = h.Sohdxuat })
                        .ToList();

                    cboHoaDon.ItemsSource = list;
                }

                cboHoaDon.DisplayMemberPath = "HienThi";
                cboHoaDon.SelectedValuePath = "Ma";
            }
        }
        private void cboHoaDon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboHoaDon.SelectedValue == null) return;
            string soHD = cboHoaDon.SelectedValue.ToString().Trim();
            string defaultKho = "";

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var k = context.Khos.FirstOrDefault();
                    if (k != null) defaultKho = k.Makho;
                    if (_currentMode == Mode.Nhap)
                    {
                        var hd = context.Hoadonnhaps.FirstOrDefault(h => h.Sohdnhap == soHD);
                        if (hd != null)
                        {
                            txtKhachHang.Text = hd.Mancc;
                            txtNgayHD.Text = hd.Ngaylap;
                            txtTongTienHD.Text = string.Format("{0:N0} đ", hd.Tongtien);
                        }

                        var query = from ct in context.Cthdnhaps
                                    where ct.Sohdnhap == soHD
                                    join sp in context.Sanphams on ct.Masp equals sp.Masp into spGroup
                                    from subSp in spGroup.DefaultIfEmpty()
                                    join lh in context.Lohangs on ct.Malo equals lh.Malo into lhGroup
                                    from subLh in lhGroup.DefaultIfEmpty()
                                    select new ChiTietView
                                    {
                                        Masp = ct.Masp,
                                        Tensp = subSp != null ? subSp.Tensp : "Sản phẩm không tồn tại",
                                        Dvt = subSp != null ? subSp.Dvt : "",
                                        Malo = ct.Malo,
                                        Hsd = (subLh != null && subLh.Hsd.HasValue)
                                              ? subLh.Hsd.Value.ToString("dd/MM/yyyy")
                                              : "---",
                                        Soluong = ct.Soluong,
                                        Dongia = ct.Dongianhap,
                                        Thanhtien = ct.Thanhtien,
                                        SelectedMakho = defaultKho
                                    };

                        _listChiTiet = query.ToList();
                    }
                    else
                    {
                        var hd = context.Hoadonxuats.FirstOrDefault(h => h.Sohdxuat == soHD);
                        if (hd != null)
                        {
                            txtKhachHang.Text = hd.Makh ?? "---";
                            txtNgayHD.Text = hd.Ngaylap ?? "---";
                            txtTongTienHD.Text = string.Format("{0:N0} đ", hd.Tongtien ?? 0);
                        }

                        var query = from ct in context.Cthdxuats
                                    where ct.Sohdxuat == soHD
                                    join sp in context.Sanphams on ct.Masp equals sp.Masp into spGroup
                                    from subSp in spGroup.DefaultIfEmpty()
                                    join lh in context.Lohangs on ct.Malo equals lh.Malo into lhGroup
                                    from subLh in lhGroup.DefaultIfEmpty()
                                    select new ChiTietView
                                    {
                                        Masp = ct.Masp,
                                        Tensp = subSp != null ? subSp.Tensp : "Sản phẩm không tồn tại",
                                        Dvt = subSp != null ? subSp.Dvt : "",
                                        Malo = ct.Malo,
                                        Hsd = (subLh != null && subLh.Hsd.HasValue)
                                              ? subLh.Hsd.Value.ToString("dd/MM/yyyy")
                                              : "---",
                                        Soluong = ct.Soluong,
                                        Dongia = ct.Dongiaban, 
                                        Thanhtien = ct.Thanhtien,
                                        SelectedMakho = defaultKho
                                    };

                        _listChiTiet = query.ToList();
                    }

 
                    dgvChiTiet.ItemsSource = null;
                    dgvChiTiet.ItemsSource = _listChiTiet;

                    if (_listChiTiet.Count == 0)
                    {
                        MessageBox.Show("Không tìm thấy sản phẩm nào trong hóa đơn này!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hiển thị chi tiết: " + ex.Message);
            }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {            
            if (cboHoaDon.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn một hóa đơn để tạo phiếu!");
                return;
            }

            string soHD = cboHoaDon.SelectedValue.ToString();
            string ngay = dpNgayLap.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");

            string khoDaiDien = (_listChiTiet.Count > 0) ? _listChiTiet[0].SelectedMakho : "";
            if (string.IsNullOrEmpty(khoDaiDien))
            {
                MessageBox.Show("Vui lòng chọn kho nhập/xuất cho các mặt hàng!");
                return;
            }

            string maNhanVien = UserSession.CurrentUser?.Manv ?? "NV01";
            string chucVu = UserSession.CurrentUser?.Chucvu ?? "";

            string[] ssep = { "Admin", "Giám đốc", "Quản lý kho" };
            bool isBoss = ssep.Any(r => r.Equals(chucVu, StringComparison.OrdinalIgnoreCase));

            string trangThaiPhieu = isBoss ? "Đã duyệt" : "Chờ duyệt";

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    if (_currentMode == Mode.Nhap)
                    {
                        var phieuHienTai = context.Phieunhaps.FirstOrDefault(p => p.Sohdnhap == soHD);

                        if (phieuHienTai != null)
                        {
                            phieuHienTai.Makho = khoDaiDien;
                            phieuHienTai.Ngaynhap = ngay;
                            phieuHienTai.Manv = maNhanVien;
                            phieuHienTai.Trangthai = trangThaiPhieu;
                        }
                        else
                        {
                            var maxPn = context.Phieunhaps
                                .Where(p => p.Mapn.StartsWith("PN"))
                                .Select(p => p.Mapn)
                                .AsEnumerable() 
                                .Select(m => int.TryParse(m.Substring(2), out int n) ? n : 0)
                                .DefaultIfEmpty(0)
                                .Max();

                            string newID = "PN" + (maxPn + 1).ToString("D3");

                            var pn = new Phieunhap
                            {
                                Mapn = newID,
                                Sohdnhap = soHD,
                                Makho = khoDaiDien,
                                Ngaynhap = ngay,
                                Manv = maNhanVien,
                                Trangthai = trangThaiPhieu
                            };
                            context.Phieunhaps.Add(pn);
                        }

                        if (isBoss)
                        {
                            var listChiTiet = context.Cthdnhaps.Where(ct => ct.Sohdnhap == soHD).ToList();
                            foreach (var item in listChiTiet)
                            {
                                var loHang = context.Lohangs.FirstOrDefault(l => l.Malo == item.Malo);
                                var hsdDef = DateOnly.FromDateTime(DateTime.Now.AddYears(2));
                                var nsxDef = DateOnly.FromDateTime(DateTime.Now);

                                if (loHang == null)
                                {
                                    context.Lohangs.Add(new Lohang { Malo = item.Malo, Masp = item.Masp, Nhacungcap = soHD, Nsx = nsxDef, Hsd = hsdDef });
                                }
                                else
                                {
                                    if (loHang.Nsx == null) loHang.Nsx = nsxDef;
                                    context.Entry(loHang).State = EntityState.Modified;
                                }


                                var tonKho = context.Tonkhos.FirstOrDefault(t => t.Masp == item.Masp && t.Malo == item.Malo && t.Makho == khoDaiDien);
                                if (tonKho != null)
                                {
                                    tonKho.Soluongton += item.Soluong;
                                    context.Entry(tonKho).State = EntityState.Modified;
                                }
                                else
                                {
                                    context.Tonkhos.Add(new Tonkho { Masp = item.Masp, Malo = item.Malo, Makho = khoDaiDien, Soluongton = item.Soluong });
                                }
                            }
                        }
                    }
                    else
                    {
                        var phieuHienTai = context.Phieuxuats.FirstOrDefault(p => p.Sohdxuat == soHD);

                        if (phieuHienTai != null)
                        {
                            phieuHienTai.Makho = khoDaiDien;
                            phieuHienTai.Ngayxuat = ngay;
                            phieuHienTai.Manv = maNhanVien;
                            phieuHienTai.Trangthai = trangThaiPhieu;
                        }
                        else
                        {
                            var maxPx = context.Phieuxuats
                                .Where(p => p.Mapx.StartsWith("PX"))
                                .Select(p => p.Mapx)
                                .AsEnumerable()
                                .Select(m => int.TryParse(m.Substring(2), out int n) ? n : 0)
                                .DefaultIfEmpty(0)
                                .Max();

                            string newID = "PX" + (maxPx + 1).ToString("D3");

                            var px = new Phieuxuat
                            {
                                Mapx = newID,
                                Sohdxuat = soHD,
                                Makho = khoDaiDien,
                                Ngayxuat = ngay,
                                Manv = maNhanVien,
                                Trangthai = trangThaiPhieu
                            };
                            context.Phieuxuats.Add(px);
                        }


                        if (isBoss)
                        {
                            var listChiTiet = context.Cthdxuats.Where(ct => ct.Sohdxuat == soHD).ToList();

                            foreach (var item in listChiTiet)
                            {
                                var tongTon = context.Tonkhos.Where(t => t.Masp == item.Masp && t.Malo == item.Malo).Sum(t => (int?)t.Soluongton) ?? 0;
                                if (tongTon < item.Soluong)
                                {
                                    MessageBox.Show($"Không đủ hàng để xuất ngay!\nSP: {item.Masp} (Lô {item.Malo})\nTồn: {tongTon} < Cần: {item.Soluong}", "Lỗi Kho");
                                    return;
                                }
                            }

                            foreach (var item in listChiTiet)
                            {
                                int canTru = item.Soluong;
                                var cacDongTon = context.Tonkhos
                                    .Where(t => t.Masp == item.Masp && t.Malo == item.Malo && t.Soluongton > 0)
                                    .OrderByDescending(t => t.Soluongton)
                                    .ToList();

                                foreach (var kho in cacDongTon)
                                {
                                    if (canTru <= 0) break;
                                    int tru = Math.Min(canTru, kho.Soluongton);

                                    kho.Soluongton -= tru;
                                    canTru -= tru;

                                    if (kho.Soluongton == 0) context.Tonkhos.Remove(kho);
                                    else context.Entry(kho).State = EntityState.Modified;
                                }
                            }
                        }
                    }
                        
                    context.SaveChanges();

                    string msg = isBoss
                        ? "Đã tạo phiếu và cập nhật kho THÀNH CÔNG (Đã duyệt)!"
                        : "Đã tạo lệnh kho và chuyển sang trạng thái CHỜ DUYỆT!";

                    MessageBox.Show(msg, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu phiếu kho: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}