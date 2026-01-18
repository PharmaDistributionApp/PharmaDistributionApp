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

        public class InventoryNotify
        {
            public string Masp { get; set; }
            public string Malo { get; set; }
            public string Message { get; set; }
            public string Type { get; set; }
            public string TenKho { get; set; }
            public string WarningColor => Type == "HSD" ? "#D32F2F" : "#EF6C00";
        }

        // Tạo một class trung gian để tránh lỗi Dynamic trong LINQ
        public class KhoHangDisplayItem
        {
            public string Masp { get; set; }
            public string Tensp { get; set; }
            public string Dvt { get; set; }
            public string TenKho { get; set; }
            public string SoHieuLo { get; set; }
            public string HSD { get; set; }
            public int SoLuongTon { get; set; }
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
                            SoHieuLo = x.subLh != null ? x.subLh.Sohieu : "---",
                            HSD = x.subLh != null ? x.subLh.Hsd.ToString() : "---",
                            SoLuongTon = x.tk.Soluongton,
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

                        resultList = query.ToList().Select(x => new KhoHangDisplayItem
                        {
                            Masp = x.pn.Mapn,
                            Tensp = "HĐ Nhập: " + x.pn.Sohdnhap,
                            TenKho = x.subK != null ? x.subK.Tenkho : x.pn.Makho,
                            HSD = x.pn.Ngaynhap,
                            Mapn = x.pn.Mapn,
                            Sohdnhap = x.pn.Sohdnhap,
                            TenTrangThai = x.pn.Trangthai,
                            LoaiRow = "PHIEUNHAP",
                            MauNenTrangThai = "#E3F2FD",
                            MauChuTrangThai = "#1565C0"
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

                        resultList = query.ToList().Select(x => new KhoHangDisplayItem
                        {
                            Masp = x.px.Mapx,
                            Tensp = "HĐ Xuất: " + x.px.Sohdxuat,
                            TenKho = x.subK != null ? x.subK.Tenkho : x.px.Makho,
                            HSD = x.px.Ngayxuat,
                            Mapx = x.px.Mapx,
                            Sohdxuat = x.px.Sohdxuat,
                            TenTrangThai = x.px.Trangthai,
                            LoaiRow = "PHIEUXUAT",
                            MauNenTrangThai = "#FCE4EC",
                            MauChuTrangThai = "#C2185B"
                        }).ToList();
                    }
                    dgvKhoHang.ItemsSource = resultList;
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message); }
        }

        private void UpdateColumnVisibility()
        {
            if (dgvKhoHang == null || dgvKhoHang.Columns.Count < 7) return;
            bool isPhieu = (_currentMode == ViewMode.PhieuNhap || _currentMode == ViewMode.PhieuXuat);
            dgvKhoHang.Columns[0].Header = isPhieu ? "Mã Phiếu" : "Mã sản phẩm";
            dgvKhoHang.Columns[1].Header = isPhieu ? "Thông tin HĐ" : "Tên sản phẩm";
            Visibility v = isPhieu ? Visibility.Collapsed : Visibility.Visible;
            dgvKhoHang.Columns[2].Visibility = v; dgvKhoHang.Columns[4].Visibility = v; dgvKhoHang.Columns[6].Visibility = v;
            dgvKhoHang.Columns[5].Header = isPhieu ? "Ngày Lập" : "Hạn Dùng";
        }

        private void dgvCanhBao_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvCanhBao.SelectedItem is InventoryNotify selected)
            {
                RadTonKho.IsChecked = true; _currentMode = ViewMode.TonKho; LoadData();
                var items = dgvKhoHang.ItemsSource as List<KhoHangDisplayItem>;
                if (items != null)
                {
                    var target = items.FirstOrDefault(x => x.Masp == selected.Masp && (string.IsNullOrEmpty(selected.Malo) || x.Malo == selected.Malo));
                    if (target != null) { dgvKhoHang.SelectedItem = target; dgvKhoHang.ScrollIntoView(target); dgvKhoHang.Focus(); }
                }
                btnBell.IsChecked = false;
            }
        }

        private void LoadCanhBaoCount()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var notifyList = new List<InventoryNotify>();
                    DateOnly warningDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
                    var maspDangNhap = (from pn in context.Phieunhaps join ct in context.Cthdnhaps on pn.Sohdnhap equals ct.Sohdnhap where pn.Trangthai != "Đã nhập" select ct.Masp).Distinct().ToList();
                    var expired = context.Lohangs.Where(l => l.Hsd != null && l.Hsd <= warningDate).Select(l => new InventoryNotify { Masp = l.Masp, Malo = l.Malo, Type = "HSD", Message = "Lô " + l.Sohieu + " sắp hết hạn" }).ToList();
                    var outOfStock = context.Sanphams.Where(sp => !maspDangNhap.Contains(sp.Masp) && context.Tonkhos.Where(t => t.Masp == sp.Masp).Sum(t => t.Soluongton) <= 5)
                        .Select(sp => new InventoryNotify { Masp = sp.Masp, Type = "SOLUONG", Message = "SP " + sp.Tensp + " tồn thấp" }).ToList();
                    notifyList.AddRange(expired); notifyList.AddRange(outOfStock);
                    dgvCanhBao.ItemsSource = notifyList;
                    txtCountBadge.Text = notifyList.Count.ToString();
                    bdBadge.Visibility = notifyList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch { }
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
        private void BtnDongPopup_Click(object sender, RoutedEventArgs e) { btnBell.IsChecked = false; }
        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e) { LoadData(); }
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e) { Keyboard.ClearFocus(); }
        private void BtnNhapHang_Click(object sender, RoutedEventArgs e) { new NhapHangWindow(_currentMode == ViewMode.PhieuXuat).ShowDialog(); LoadData(); }
        private string Helper_FormatDate(object input) => input?.ToString() ?? "---";
        private void BtnSua_Click(object sender, RoutedEventArgs e) { }
        private void BtnXoa_Click(object sender, RoutedEventArgs e) { }
        private void dgvKhoHang_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Lấy dòng dữ liệu đang chọn và ép kiểu về Class DisplayItem đã tạo
            var row = dgvKhoHang.SelectedItem as KhoHangDisplayItem;
            if (row == null) return;

            try
            {
                if (_currentMode == ViewMode.PhieuNhap)
                {
                    // Mở chi tiết phiếu nhập (truyền Mapn)
                    if (!string.IsNullOrEmpty(row.Mapn))
                    {
                        ChiTietPhieuNhapWindow window = new ChiTietPhieuNhapWindow(row.Mapn);
                        window.ShowDialog();
                    }
                }
                else if (_currentMode == ViewMode.PhieuXuat)
                {
                    // Mở chi tiết phiếu xuất (truyền Mapx)
                    if (!string.IsNullOrEmpty(row.Mapx))
                    {
                        ChiTietPhieuXuatWindow window = new ChiTietPhieuXuatWindow(row.Mapx);
                        window.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi mở chi tiết: " + ex.Message);
            }
        }
        private void BtnHanhDong_Click(object sender, RoutedEventArgs e) { }
    }
}