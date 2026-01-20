using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PharmaDistributionApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PharmaDistributionApp.Views.KhoHangView;
using PharmaDistributionApp.Services;

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class KhoHangControl : UserControl
    {
        private enum ViewMode { TonKho, PhieuNhap, PhieuXuat }
        private ViewMode _currentMode = ViewMode.TonKho;

        public KhoHangControl()
        {
            InitializeComponent();
            LoadData();
            LoadCanhBaoCount();
            UpdateColumnVisibility();
        }

        public class ThongBaoItem
        {
            public string LoaiThongBao { get; set; }
            public string TieuDe { get; set; }
            public string NoiDung { get; set; }
            public string ChiTiet { get; set; }
            public string ThoiGian { get; set; }
            public string MaRef { get; set; }
            public string MaKhoRef { get; set; }
            public DateTime SortDate { get; set; }
            public string IconKind { get; set; }
            public string Color { get; set; }
            public string BgColor { get; set; }
        }

        public class KhoHangDisplayItem
        {
            public string Masp { get; set; }
            public string Tensp { get; set; }
            public string Dvt { get; set; }
            public string TenKho { get; set; }
            public string Makho { get; set; }
            public string HSD { get; set; }
            public decimal SoLuongTon { get; set; }
            public string TenTrangThai { get; set; }
            public string MauNenTrangThai { get; set; }
            public string MauChuTrangThai { get; set; }
            public string LoaiRow { get; set; }
            public string Mapn { get; set; }
            public string Sohdnhap { get; set; }
            public string Mapx { get; set; }
            public string Sohdxuat { get; set; }
            public string Malo { get; set; }
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (RadTonKho.IsChecked == true) _currentMode = ViewMode.TonKho;
            else if (RadPhieuNhap.IsChecked == true) _currentMode = ViewMode.PhieuNhap;
            else if (RadPhieuXuat.IsChecked == true) _currentMode = ViewMode.PhieuXuat;
            LoadData();
            UpdateColumnVisibility();
        }

        private void LoadData()
        {
            if (dgvKhoHang == null) return;
            string keyword = txtTimKiem.Text.ToLower().Trim();
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var resultList = new List<KhoHangDisplayItem>();
                    if (_currentMode == ViewMode.TonKho)
                    {
                        var query = from tk in context.Tonkhos
                                    join sp in context.Sanphams on tk.Masp equals sp.Masp
                                    join k in context.Khos on tk.Makho equals k.Makho
                                    join lh in context.Lohangs on tk.Malo equals lh.Malo into lhGroup
                                    from subLh in lhGroup.DefaultIfEmpty()
                                    select new { tk, sp, k, subLh };

                        if (!string.IsNullOrEmpty(keyword))
                            query = query.Where(x => x.sp.Masp.ToLower().Contains(keyword) || x.sp.Tensp.ToLower().Contains(keyword));

                        resultList = query.ToList().Select(x => new KhoHangDisplayItem
                        {
                            Masp = x.sp.Masp,
                            Tensp = x.sp.Tensp,
                            Dvt = x.sp.Dvt,
                            TenKho = x.k.Tenkho,
                            Makho = x.tk.Makho,
                            HSD = x.subLh != null ? x.subLh.Hsd.ToString() : "---",
                            SoLuongTon = (int)x.tk.Soluongton,
                            Malo = x.tk.Malo,
                            TenTrangThai = x.tk.Soluongton == 0 ? "Hết hàng" : (x.tk.Soluongton <= 10 ? "Sắp hết" : "Còn hàng"),
                            MauNenTrangThai = x.tk.Soluongton == 0 ? "#FFEBEE" : (x.tk.Soluongton <= 10 ? "#FFF3E0" : "#E8F5E9"),
                            MauChuTrangThai = x.tk.Soluongton == 0 ? "#C62828" : (x.tk.Soluongton <= 10 ? "#EF6C00" : "#2E7D32"),
                            LoaiRow = "TONKHO"
                        }).ToList();
                    }
                    else if (_currentMode == ViewMode.PhieuNhap)
                    {
                        var query = from pn in context.Phieunhaps
                                    join k in context.Khos on pn.Makho equals k.Makho into kGroup
                                    from subK in kGroup.DefaultIfEmpty()
                                    select new { pn, subK };

                        if (!string.IsNullOrEmpty(keyword))
                            query = query.Where(x => x.pn.Mapn.ToLower().Contains(keyword) || x.pn.Sohdnhap.ToLower().Contains(keyword));
                        var rawList = query.ToList();

                            resultList = rawList.Select(x => {
                                decimal tongTien = context.Cthdnhaps
                                                .Where(ct => ct.Sohdnhap == x.pn.Sohdnhap)
                                                .ToList()
                                                .Sum(ct => ct.Thanhtien);
                                string ngayHienThi = x.pn.Ngaynhap;
                                if (DateTime.TryParse(x.pn.Ngaynhap, out DateTime dt)) ngayHienThi = dt.ToString("dd/MM/yyyy");
                                
                                return new KhoHangDisplayItem
                                {
                                Masp = x.pn.Mapn,
                                Tensp = ngayHienThi,
                                TenKho = x.subK != null ? x.subK.Tenkho : x.pn.Makho,
                                HSD = x.pn.Ngaynhap,
                                Mapn = x.pn.Mapn,
                                Sohdnhap = x.pn.Sohdnhap,
                                SoLuongTon = (int)tongTien,

                                TenTrangThai = x.pn.Trangthai,
                                LoaiRow = "PHIEUNHAP",
                                MauNenTrangThai = "#E3F2FD",
                                MauChuTrangThai = "#1565C0"
                            };
                        }).ToList();
                    }
                    else if (_currentMode == ViewMode.PhieuXuat)
                    {
                        var query = from px in context.Phieuxuats
                                    join k in context.Khos on px.Makho equals k.Makho into kGroup
                                    from subK in kGroup.DefaultIfEmpty()
                                    select new { px, subK };

                        if (!string.IsNullOrEmpty(keyword))
                            query = query.Where(x => x.px.Mapx.ToLower().Contains(keyword) || x.px.Sohdxuat.ToLower().Contains(keyword));

                        var rawList = query.ToList();

                        resultList = rawList.Select(x => {
                            decimal tongTien = context.Cthdxuats
                                                .Where(ct => ct.Sohdxuat == x.px.Sohdxuat)
                                                .ToList()
                                                .Sum(ct => ct.Thanhtien);
                            string ngayHienThi = x.px.Ngayxuat;
                            if (DateTime.TryParse(x.px.Ngayxuat, out DateTime dt)) ngayHienThi = dt.ToString("dd/MM/yyyy");

                            return new KhoHangDisplayItem
                            {
                                Masp = x.px.Mapx,
                                Tensp = ngayHienThi,
                                TenKho = x.subK != null ? x.subK.Tenkho : x.px.Makho,
                                HSD = x.px.Ngayxuat,
                                Mapx = x.px.Mapx,
                                Sohdxuat = x.px.Sohdxuat,
                                SoLuongTon = (int)tongTien,

                                TenTrangThai = x.px.Trangthai,
                                LoaiRow = "PHIEUXUAT",
                                MauNenTrangThai = "#FCE4EC",
                                MauChuTrangThai = "#C2185B"
                            };
                        }).ToList();
                    }
                    dgvKhoHang.ItemsSource = resultList;
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message); }
        }

        private void UpdateColumnVisibility()
        {
            if (dgvKhoHang == null || dgvKhoHang.Columns.Count < 8) return;
            bool isPhieu = (_currentMode == ViewMode.PhieuNhap || _currentMode == ViewMode.PhieuXuat);
            dgvKhoHang.Columns[1].Header = isPhieu ? "Mã phiếu" : "Mã sản phẩm";
            dgvKhoHang.Columns[2].Header = isPhieu ? "Thời gian tạo" : "Tên sản phẩm";
            dgvKhoHang.Columns[5].Header = isPhieu ? "Tổng tiền" : "Tồn kho";
            Visibility productMode = isPhieu ? Visibility.Collapsed : Visibility.Visible;
            dgvKhoHang.Columns[0].Visibility = productMode; 
            dgvKhoHang.Columns[3].Visibility = productMode;
            dgvKhoHang.Columns[4].Visibility = productMode; 
            dgvKhoHang.Columns[1].Visibility = Visibility.Visible; 
            dgvKhoHang.Columns[2].Visibility = Visibility.Visible; 
            dgvKhoHang.Columns[5].Visibility = Visibility.Visible;
            dgvKhoHang.Columns[6].Visibility = Visibility.Visible;
            Visibility actionVisibility = Visibility.Collapsed; 

            if (UserSession.CurrentUser != null)
            {
                string[] rolesDuocPhep = { "Admin", "Giám đốc", "Quản lý kho" };
                if (rolesDuocPhep.Contains(UserSession.CurrentUser.Chucvu))
                {
                    actionVisibility = Visibility.Visible;
                }
            }
            dgvKhoHang.Columns[7].Visibility = actionVisibility;
        }

        private void LoadCanhBaoCount()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var notiList = new List<ThongBaoItem>();

                    // --- [MỚI] PHẦN XỬ LÝ YÊU CẦU TỪ HÓA ĐƠN ---
                    // 1. Tìm yêu cầu Nhập kho (Trạng thái chứa 'Yêu cầu từ HD')
                    var listReqNhap = context.Phieunhaps.ToList()
                        .Where(p => !string.IsNullOrEmpty(p.Trangthai) &&
                                   (p.Trangthai.Contains("Yêu cầu từ HD") || p.Trangthai.Contains("Cập nhật từ HD")))
                        .ToList();

                    foreach (var p in listReqNhap)
                    {
                        notiList.Add(new ThongBaoItem
                        {
                            LoaiThongBao = "REQ_NHAP_KHO",
                            MaRef = p.Mapn,
                            TieuDe = "Yêu cầu nhập kho",
                            NoiDung = $"Hóa đơn: {p.Sohdnhap}",
                            ChiTiet = "Cần tạo phiếu chi tiết",
                            ThoiGian = DateTime.Now.ToString("HH:mm dd/MM"),
                            SortDate = DateTime.Now.AddDays(1), // Ưu tiên hiển thị đầu
                            IconKind = "TruckDelivery",
                            Color = "#FFFFFF",
                            BgColor = "#2962FF" // Xanh đậm
                        });
                    }

                    // 2. Tìm yêu cầu Xuất kho
                    var listReqXuat = context.Phieuxuats.ToList()
                        .Where(p => !string.IsNullOrEmpty(p.Trangthai) &&
                                   (p.Trangthai.Contains("Yêu cầu từ HD") || p.Trangthai.Contains("Cập nhật từ HD")))
                        .ToList();

                    foreach (var p in listReqXuat)
                    {
                        notiList.Add(new ThongBaoItem
                        {
                            LoaiThongBao = "REQ_XUAT_KHO",
                            MaRef = p.Mapx,
                            TieuDe = "Yêu cầu xuất kho",
                            NoiDung = $"Hóa đơn: {p.Sohdxuat}",
                            ChiTiet = "Cần xuất hàng ngay",
                            ThoiGian = DateTime.Now.ToString("HH:mm dd/MM"),
                            SortDate = DateTime.Now.AddDays(1),
                            IconKind = "Dolly",
                            Color = "#FFFFFF",
                            BgColor = "#FF6D00" // Cam đậm
                        });
                    }
                    // --- [KẾT THÚC PHẦN MỚI] ---

                    // --- [LOGIC CŨ - GIỮ NGUYÊN] ---
                    var listNhap = context.Phieunhaps.ToList()
                        .Where(p => !string.IsNullOrEmpty(p.Trangthai) && p.Trangthai.ToLower().Contains("chờ duyệt"))
                        .ToList();

                    foreach (var p in listNhap)
                    {
                        decimal tong = context.Cthdnhaps.Where(c => c.Sohdnhap == p.Sohdnhap).ToList().Sum(c => c.Thanhtien);
                        notiList.Add(new ThongBaoItem
                        {
                            LoaiThongBao = "PHIEU_NHAP",
                            MaRef = p.Mapn,
                            TieuDe = p.Mapn,
                            NoiDung = "Nhập kho chờ duyệt",
                            ChiTiet = tong.ToString("N0") + " đ",
                            ThoiGian = p.Ngaynhap ?? "---",
                            SortDate = DateTime.Now,
                            IconKind = "FileImport",
                            Color = "#1565C0",
                            BgColor = "#E3F2FD"
                        });
                    }

                    var listXuat = context.Phieuxuats.ToList()
                        .Where(p => !string.IsNullOrEmpty(p.Trangthai) && p.Trangthai.ToLower().Contains("chờ duyệt"))
                        .ToList();

                    foreach (var p in listXuat)
                    {
                        decimal tong = context.Cthdxuats.Where(c => c.Sohdxuat == p.Sohdxuat).ToList().Sum(c => c.Thanhtien);
                        notiList.Add(new ThongBaoItem
                        {
                            LoaiThongBao = "PHIEU_XUAT",
                            MaRef = p.Mapx,
                            TieuDe = p.Mapx,
                            NoiDung = "Xuất kho chờ duyệt",
                            ChiTiet = tong.ToString("N0") + " đ",
                            ThoiGian = p.Ngayxuat ?? "---",
                            SortDate = DateTime.Now,
                            IconKind = "FileExport",
                            Color = "#2E7D32",
                            BgColor = "#E8F5E9"
                        });
                    }

                    var homNay = DateOnly.FromDateTime(DateTime.Now);

                    var listHetHan = (from t in context.Tonkhos
                                      join l in context.Lohangs on t.Malo equals l.Malo
                                      where l.Hsd < homNay && t.Soluongton > 0
                                      select new { t, l }).ToList();

                    foreach (var item in listHetHan)
                    {
                        var sp = context.Sanphams.FirstOrDefault(s => s.Masp == item.t.Masp);
                        if (sp == null) continue; // Bỏ qua rác

                        var kho = context.Khos.FirstOrDefault(k => k.Makho == item.t.Makho);
                        string tenKho = kho != null ? kho.Tenkho : item.t.Makho;

                        notiList.Add(new ThongBaoItem
                        {
                            LoaiThongBao = "CANH_BAO_HET_HAN",
                            MaRef = item.t.Masp,
                            MaKhoRef = item.t.Malo,
                            TieuDe = item.t.Masp,
                            NoiDung = $"ĐÃ HẾT HẠN! - {sp.Tensp}",
                            ChiTiet = $"Lô: {item.t.Malo}",
                            ThoiGian = item.l.Hsd.HasValue ? item.l.Hsd.Value.ToString("dd/MM/yyyy") : "---",
                            SortDate = DateTime.Now.AddDays(-1),
                            IconKind = "CalendarRemove",
                            Color = "#D32F2F",
                            BgColor = "#212121"
                        });
                    }

                    var listTonKho = context.Tonkhos
                        .Where(t => t.Soluongton <= 10)
                        .ToList();

                    foreach (var t in listTonKho)
                    {
                        var sp = context.Sanphams.FirstOrDefault(s => s.Masp == t.Masp);
                        var kho = context.Khos.FirstOrDefault(k => k.Makho == t.Makho);

                        if (sp == null) continue;

                        string tenSP = sp.Tensp;
                        string dvt = sp.Dvt;
                        string tenKho = kho != null ? kho.Tenkho : t.Makho;
                        string loaiTB, icon, color, bgColor, noiDungTB;

                        if (t.Soluongton == 0)
                        {
                            loaiTB = "CANH_BAO_HET";
                            icon = "AlertOctagon";
                            color = "#C62828";
                            bgColor = "#FFEBEE";
                            noiDungTB = "Đã hết hàng!";
                        }
                        else
                        {
                            loaiTB = "CANH_BAO_SAP_HET";
                            icon = "AlertCircle";
                            color = "#EF6C00";
                            bgColor = "#FFF3E0";
                            noiDungTB = "Sắp hết hàng";
                        }

                        notiList.Add(new ThongBaoItem
                        {
                            LoaiThongBao = loaiTB,
                            MaRef = t.Masp,
                            MaKhoRef = t.Malo,
                            TieuDe = t.Masp,
                            NoiDung = $"{noiDungTB} - {tenSP}",
                            ChiTiet = $"Tồn: {t.Soluongton} {dvt}",
                            ThoiGian = tenKho,
                            SortDate = DateTime.Now.AddDays(-1),
                            IconKind = icon,
                            Color = color,
                            BgColor = bgColor
                        });
                    }
                    var finalData = notiList.OrderByDescending(x => x.SortDate).ToList();
                    lbThongBao.ItemsSource = finalData;

                    if (finalData.Count > 0)
                    {
                        bdBadge.Visibility = Visibility.Visible;
                        txtCountBadge.Text = finalData.Count.ToString();
                    }
                    else
                    {
                        bdBadge.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải thông báo: " + ex.Message);
            }
        }

        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelPackage.License.SetNonCommercialPersonal("PharmaApp");
            var listData = dgvKhoHang.ItemsSource as List<KhoHangDisplayItem>;
            if (listData == null) return;
            SaveFileDialog sfd = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = "BaoCaoKho" };
            if (sfd.ShowDialog() == true)
            {
                using (var p = new ExcelPackage())
                {
                    var ws = p.Workbook.Worksheets.Add("Kho");
                    ws.Cells["A1"].Value = "Mã"; ws.Cells["B1"].Value = "Tên"; ws.Cells["C1"].Value = "Kho"; ws.Cells["D1"].Value = "Tồn";
                    int r = 2;
                    foreach (var i in listData) { ws.Cells[r, 1].Value = i.Masp; ws.Cells[r, 2].Value = i.Tensp; ws.Cells[r, 3].Value = i.TenKho; ws.Cells[r, 4].Value = i.SoLuongTon; r++; }
                    File.WriteAllBytes(sfd.FileName, p.GetAsByteArray());
                    MessageBox.Show("Xong!");
                }
            }
        }

        private void BtnThongBao_Click(object sender, RoutedEventArgs e) { if (btnBell.IsChecked == true) LoadCanhBaoCount(); }
        private void BtnDongPopup_Click(object sender, RoutedEventArgs e)
        {
            btnBell.IsChecked = false;
        }
        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e) { LoadData(); }
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e) 
        {
            if (!dgvKhoHang.IsMouseOver)
            {
                dgvKhoHang.UnselectAll(); 
                Keyboard.ClearFocus();    
            }

            if (!btnBell.IsMouseOver && btnBell.IsChecked == true)
            {
                btnBell.IsChecked = false;
            }
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && btnBell.IsChecked == true)
            {
                btnBell.IsChecked = false; 
                this.Focus();
            }
        }
        private void BtnNhapHang_Click(object sender, RoutedEventArgs e) { new NhapHangWindow(_currentMode == ViewMode.PhieuXuat).ShowDialog(); LoadData(); }
        private string Helper_FormatDate(object input) => input?.ToString() ?? "---";
        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menuItem = sender as MenuItem;
                if (menuItem == null) return;

                var contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu == null) return;
                var btn = contextMenu.PlacementTarget as Button;
                if (btn == null) return;
                var rowData = btn.DataContext as KhoHangDisplayItem;
                if (rowData == null) return;
  
                if (_currentMode == ViewMode.TonKho)
                {
                    var editWindow = new ChinhSuaTonKho(rowData.Masp, rowData.Malo, rowData.Makho);
                    editWindow.ShowDialog();
                    LoadData();
                }
                else
                {
                    bool isXuat = (_currentMode == ViewMode.PhieuXuat);
                    string maPhieu = isXuat ? rowData.Mapx : rowData.Mapn;

                    if (string.IsNullOrEmpty(maPhieu))
                    {
                        MessageBox.Show("Không tìm thấy mã phiếu để chỉnh sửa!");
                        return;
                    }
                    var editPhieuWindow = new ChinhSuaPhieuWindow(maPhieu, isXuat);
                    editPhieuWindow.ShowDialog();
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi mở cửa sổ chỉnh sửa: " + ex.Message);
            }
        }
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            var contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu == null) return;

            var btn = contextMenu.PlacementTarget as Button;
            var rowData = btn.DataContext as KhoHangDisplayItem;

            if (rowData != null)
            {
                string maCanXoa = _currentMode == ViewMode.TonKho ? rowData.Masp : (_currentMode == ViewMode.PhieuNhap ? rowData.Mapn : rowData.Mapx);

                if (MessageBox.Show($"Bạn có chắc muốn xóa '{maCanXoa}' không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    MessageBox.Show("Đã gửi yêu cầu xóa! (Chức năng cần kết nối Database)");
                }
            }
        }
        private void dgvKhoHang_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = dgvKhoHang.SelectedItem as KhoHangDisplayItem;
            if (row == null) return;

            try
            {
                if (_currentMode == ViewMode.TonKho)
                {
                    if (!string.IsNullOrEmpty(row.Masp))
                    {
                        var detailWindow = new ChiTietTonKhoWindow(row.Masp);
                        detailWindow.ShowDialog();
                    }
                }
                else if (_currentMode == ViewMode.PhieuNhap)
                {
                    if (!string.IsNullOrEmpty(row.Mapn))
                    {
                        ChiTietPhieuNhapWindow window = new ChiTietPhieuNhapWindow(row.Mapn);
                        window.ShowDialog();
                        LoadData();
                    }
                }
                else if (_currentMode == ViewMode.PhieuXuat)
                {
                    if (!string.IsNullOrEmpty(row.Mapx))
                    {
                        ChiTietPhieuXuatWindow window = new ChiTietPhieuXuatWindow(row.Mapx);
                        window.ShowDialog();
                        LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi mở chi tiết: " + ex.Message);
            }
        }

        private void lbThongBao_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = lbThongBao.SelectedItem as ThongBaoItem;
            if (item == null || string.IsNullOrEmpty(item.LoaiThongBao)) return;
            btnBell.IsChecked = false;
            string loai = item.LoaiThongBao.ToUpper();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (loai == "PHIEU_NHAP")
                {
                    RadPhieuNhap.IsChecked = true;
                    _currentMode = ViewMode.PhieuNhap;
                    LoadData();
                    var window = new ChiTietPhieuNhapWindow(item.MaRef);
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();

                    LoadData();
                }
                else if (loai == "PHIEU_XUAT")
                {
                    RadPhieuXuat.IsChecked = true;
                    _currentMode = ViewMode.PhieuXuat;
                    LoadData();
                    var window = new ChiTietPhieuXuatWindow(item.MaRef);
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();

                    LoadData();
                }
                else if (loai.Contains("CANH_BAO"))
                {
                    RadTonKho.IsChecked = true;
                    _currentMode = ViewMode.TonKho;
                    txtTimKiem.Text = "";
                    LoadData();
                    UpdateColumnVisibility();
                    var list = dgvKhoHang.ItemsSource as List<KhoHangDisplayItem>;
                    if (list != null)
                    {
                        var target = list.FirstOrDefault(x => x.Masp == item.MaRef && x.Malo == item.MaKhoRef);
                        if (target != null)
                        {
                            dgvKhoHang.SelectedItem = target;
                            dgvKhoHang.UpdateLayout();
                            dgvKhoHang.ScrollIntoView(target);
                            dgvKhoHang.Focus();
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
            e.Handled = true;
        }

        private void NotificationItem_Click(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var item = element?.DataContext as ThongBaoItem;
            if (item == null) return;
            btnBell.IsChecked = false;
            string loai = (item.LoaiThongBao ?? "").ToUpper();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (loai == "PHIEU_NHAP")
                {
                    RadPhieuNhap.IsChecked = true;
                    _currentMode = ViewMode.PhieuNhap;
                    LoadData();

                    var window = new ChiTietPhieuNhapWindow(item.MaRef);
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog(); 
                    LoadData();         
                    LoadCanhBaoCount();   
                }
                else if (loai == "PHIEU_XUAT")
                {
                    RadPhieuXuat.IsChecked = true;
                    _currentMode = ViewMode.PhieuXuat;
                    LoadData();

                    var window = new ChiTietPhieuXuatWindow(item.MaRef);
                    window.Owner = Window.GetWindow(this);
                    window.ShowDialog();
                    LoadData();
                    LoadCanhBaoCount();
                }
                else if (loai.Contains("CANH_BAO"))
                {
                    RadTonKho.IsChecked = true;
                    _currentMode = ViewMode.TonKho;
                    txtTimKiem.Text = "";
                    LoadData();
                    LoadCanhBaoCount(); 
                    UpdateColumnVisibility();
                    var list = dgvKhoHang.ItemsSource as List<KhoHangDisplayItem>;
                    if (list != null)
                    {
                        var target = list.FirstOrDefault(x => x.Masp == item.MaRef && x.Malo == item.MaKhoRef);
                        if (target == null) target = list.FirstOrDefault(x => x.Masp == item.MaRef);

                        if (target != null)
                        {
                            dgvKhoHang.SelectedItem = target;
                            dgvKhoHang.UpdateLayout();
                            dgvKhoHang.ScrollIntoView(target);
                            dgvKhoHang.Focus();
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        private void BtnHanhDong_Click(object sender, RoutedEventArgs e) 
        {
            var btn = sender as Button;
            if (btn != null && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn; 
                btn.ContextMenu.IsOpen = true; 
            }
        }
    }
}