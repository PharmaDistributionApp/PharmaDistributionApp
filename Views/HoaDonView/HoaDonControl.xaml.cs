using ClosedXML.Excel;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PharmaDistributionApp.Views.Controls
{
    public partial class HoaDonControl : UserControl
    {
        private string _currentTab = "Xuat";
        private decimal _maxInvoiceValue = 100000000;
        private bool _isSyncing = false;
        private bool _isSortAscending = false;

        // Danh sách các chức vụ có quyền quản lý (Sếp)
        private readonly string[] _approverRoles = { "Admin", "Quản lý", "Giám đốc", "Kế toán", "ADMIN", "QuanLy", "TruongKho" };

        public HoaDonControl()
        {
            InitializeComponent();
            EnsureTableStructure();
            InitFilterData();

            this.Loaded += (s, e) =>
            {
                LoadDataFromDatabase(true);
                LoadNotifications();
            };

            UpdateTabVisuals();
        }

        private void EnsureTableStructure()
        {
            try
            {
                using (var conn = new SQLiteConnection("Data Source=PharmaDB.db"))
                {
                    conn.Open();
                    try { new SQLiteCommand("ALTER TABLE HOADONXUAT ADD COLUMN PheDuyet INTEGER DEFAULT 0", conn).ExecuteNonQuery(); } catch { }
                    try { new SQLiteCommand("ALTER TABLE HOADONNHAP ADD COLUMN PheDuyet INTEGER DEFAULT 0", conn).ExecuteNonQuery(); } catch { }
                }
            }
            catch { }
        }

        // --- 1. HIỂN THỊ DỮ LIỆU ---
        private void LoadDataFromDatabase(bool updateSlider = false)
        {
            if (dgHoaDon == null) return;
            try
            {
                string table = _currentTab == "Xuat" ? "HOADONXUAT" : "HOADONNHAP";
                string partner = _currentTab == "Xuat" ? "KHACHHANG" : "NHACUNGCAP";
                string colPartner = _currentTab == "Xuat" ? "MAKH" : "MANCC";
                string namePartner = _currentTab == "Xuat" ? "TENKH" : "TENNCC";
                string colID = _currentTab == "Xuat" ? "SOHDXUAT" : "SOHDNHAP";

                // Lấy tất cả (kể cả chờ duyệt)
                string sql = $@"
                    SELECT H.{colID} AS MaHD, 
                           IFNULL(P.{namePartner}, 'Khách lẻ/Vãng lai') AS DoiTac, 
                           H.NGAYLAP, H.TONGTIEN, H.TRANGTHAI, H.PheDuyet
                    FROM {table} H 
                    LEFT JOIN {partner} P ON H.{colPartner} = P.{colPartner}";

                var dt = Database.GetTable(sql);
                var list = new List<InvoiceViewModel>();

                foreach (DataRow r in dt.Rows)
                {
                    list.Add(new InvoiceViewModel
                    {
                        MaHD = r["MaHD"].ToString(),
                        DoiTac = r["DoiTac"].ToString(),
                        NgayLap = DateTime.Parse(r["NGAYLAP"].ToString()),
                        TongTien = Convert.ToDecimal(r["TONGTIEN"]),
                        TrangThai = r["TRANGTHAI"].ToString(),
                        PheDuyet = Convert.ToInt32(r["PheDuyet"]),
                        LoaiHD = _currentTab == "Xuat" ? "Xuất" : "Nhập"
                    });
                }

                if (updateSlider && list.Any()) UpdateMaxValue((double)list.Max(x => x.TongTien));

                // Filter logic...
                var filtered = list.AsEnumerable();
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string k = txtSearch.Text.ToLower();
                    filtered = filtered.Where(x => x.MaHD.ToLower().Contains(k) || x.DoiTac.ToLower().Contains(k));
                }
                if (cbbStatus.SelectedItem is ComboBoxItem item && item.Tag?.ToString() != "All")
                    filtered = filtered.Where(x => x.TrangThai == item.Tag.ToString());

                // Sort logic...
                string sortType = (cbbSortCriteria.SelectedItem as ComboBoxItem)?.Tag.ToString();
                if (sortType == "TongTien") filtered = _isSortAscending ? filtered.OrderBy(x => x.TongTien) : filtered.OrderByDescending(x => x.TongTien);
                else filtered = _isSortAscending ? filtered.OrderBy(x => x.NgayLap) : filtered.OrderByDescending(x => x.NgayLap);

                dgHoaDon.ItemsSource = filtered.ToList();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message); }
        }

        // --- 2. QUYỀN SỬA ---
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvoiceViewModel item)
            {
                if (!IsBoss())
                {
                    MessageBox.Show("Nhân viên chỉ được tạo mới.\nKhông có quyền sửa hóa đơn.", "Cấm truy cập", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                // Sếp được sửa mọi thứ
                var editWindow = new EditInvoiceWindow(item);
                if (editWindow.ShowDialog() == true)
                {
                    LoadDataFromDatabase();
                    LoadNotifications();
                }
            }
        }

        // --- 3. QUYỀN XÓA ---
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvoiceViewModel item)
            {
                if (!IsBoss())
                {
                    MessageBox.Show("Nhân viên không có quyền xóa hóa đơn.", "Cấm truy cập", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                if (MessageBox.Show($"Xóa VĨNH VIỄN hóa đơn {item.MaHD}?\n(Hành động này không thể hoàn tác)",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    DeleteInvoicePermanently(item);
                }
            }
        }

        private bool IsBoss()
        {
            if (UserSession.CurrentUser == null || string.IsNullOrEmpty(UserSession.CurrentUser.Chucvu)) return false;
            return _approverRoles.Any(r => r.Equals(UserSession.CurrentUser.Chucvu.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private void DeleteInvoicePermanently(InvoiceViewModel item)
        {
            bool isExport = item.LoaiHD == "Xuất" || item.MaHD.StartsWith("HDX");
            string tblH = isExport ? "HOADONXUAT" : "HOADONNHAP";
            string tblD = isExport ? "CTHDXUAT" : "CTHDNHAP";
            string colID = isExport ? "SOHDXUAT" : "SOHDNHAP";

            using (var conn = new SQLiteConnection("Data Source=PharmaDB.db"))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Nếu hóa đơn ĐÃ DUYỆT (PheDuyet=0) -> Phải hoàn trả kho trước khi xóa
                        //    - Xóa đơn XUẤT (đã trừ kho) -> Phải CỘNG lại (+)
                        //    - Xóa đơn NHẬP (đã cộng kho) -> Phải TRỪ đi (-)
                        if (item.PheDuyet == 0)
                        {
                            string op = isExport ? "+" : "-";
                            string sqlStock = $"SELECT MALO, SOLUONG FROM {tblD} WHERE {colID} = @id";

                            using (var cmdGet = new SQLiteCommand(sqlStock, conn, trans))
                            {
                                cmdGet.Parameters.AddWithValue("@id", item.MaHD);
                                using (var reader = cmdGet.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        string ml = reader["MALO"].ToString();
                                        int sl = Convert.ToInt32(reader["SOLUONG"]);
                                        // Cập nhật kho
                                        new SQLiteCommand($"UPDATE TONKHO SET SOLUONGTON = SOLUONGTON {op} {sl} WHERE MALO='{ml}'", conn, trans).ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        // 2. Xóa Dữ liệu (Chi tiết trước, Header sau)
                        var cmdDelD = new SQLiteCommand($"DELETE FROM {tblD} WHERE {colID}=@id", conn, trans);
                        cmdDelD.Parameters.AddWithValue("@id", item.MaHD);
                        cmdDelD.ExecuteNonQuery();

                        var cmdDelH = new SQLiteCommand($"DELETE FROM {tblH} WHERE {colID}=@id", conn, trans);
                        cmdDelH.Parameters.AddWithValue("@id", item.MaHD);
                        cmdDelH.ExecuteNonQuery();

                        // 3. Xóa luôn yêu cầu sửa nếu có (dọn rác)
                        new SQLiteCommand($"DELETE FROM YEUCAU_SUA WHERE MAHD='{item.MaHD}'", conn, trans).ExecuteNonQuery();

                        trans.Commit();
                        MessageBox.Show("Đã xóa hóa đơn thành công!");

                        // Refresh UI
                        LoadDataFromDatabase();
                        LoadNotifications();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show("Lỗi khi xóa: " + ex.Message);
                    }
                }
            }
        }

        // --- 4. THÔNG BÁO ---
        private void LoadNotifications()
        {
            if (!IsBoss()) { gridNotification.Visibility = Visibility.Collapsed; return; }
            gridNotification.Visibility = Visibility.Visible;

            try
            {
                // Lấy các hóa đơn có PheDuyet > 0
                string sql = @"
                    SELECT SOHDNHAP AS Ma, (SELECT TENNCC FROM NHACUNGCAP WHERE MANCC = H.MANCC) AS DoiTac, NGAYLAP, TONGTIEN, PheDuyet, TRANGTHAI, 'Nhập' AS LoaiReal FROM HOADONNHAP H WHERE PheDuyet > 0
                    UNION ALL
                    SELECT SOHDXUAT AS Ma, (SELECT TENKH FROM KHACHHANG WHERE MAKH = H.MAKH) AS DoiTac, NGAYLAP, TONGTIEN, PheDuyet, TRANGTHAI, 'Xuất' AS LoaiReal FROM HOADONXUAT H WHERE PheDuyet > 0
                    ORDER BY NGAYLAP DESC";

                var dt = Database.GetTable(sql);
                var list = new List<InvoiceViewModel>();
                foreach (DataRow r in dt.Rows)
                {
                    int pFlag = Convert.ToInt32(r["PheDuyet"]);
                    list.Add(new InvoiceViewModel
                    {
                        MaHD = r["Ma"].ToString(),
                        DoiTac = r["DoiTac"].ToString(),
                        NgayLap = DateTime.Parse(r["NGAYLAP"].ToString()),
                        TongTien = Convert.ToDecimal(r["TONGTIEN"]),
                        PheDuyet = pFlag,
                        TrangThai = "Chờ duyệt",
                        LoaiHD = r["LoaiReal"].ToString()
                    });
                }

                lvPendingInvoices.ItemsSource = list;
                if (list.Count > 0) { bdBadge.Visibility = Visibility.Visible; txtBadgeCount.Text = list.Count.ToString(); }
                else { bdBadge.Visibility = Visibility.Collapsed; }

            }
            catch { }
        }

        private void lvPendingInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvPendingInvoices.SelectedItem is InvoiceViewModel item)
            {
                if (btnNoti != null) btnNoti.IsChecked = false;
                if (new ChiTietHoaDonWindow(item).ShowDialog() == true)
                {
                    LoadDataFromDatabase();
                    LoadNotifications();
                }
            }
        }

        // --- 5. XUẤT EXCEL ---
        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var listData = dgHoaDon.ItemsSource as List<InvoiceViewModel>;
            if (listData == null || listData.Count == 0) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = $"DanhSachHoaDon_{DateTime.Now:ddMMyyyy}.xlsx" };
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Data");
                        worksheet.Cell(1, 1).Value = "Mã HĐ"; worksheet.Cell(1, 2).Value = "Đối tác";
                        worksheet.Cell(1, 3).Value = "Ngày lập"; worksheet.Cell(1, 4).Value = "Tổng tiền";
                        worksheet.Cell(1, 5).Value = "Trạng thái"; worksheet.Cell(1, 6).Value = "Loại";
                        int row = 2;
                        foreach (var item in listData)
                        {
                            worksheet.Cell(row, 1).Value = item.MaHD; worksheet.Cell(row, 2).Value = item.DoiTac;
                            worksheet.Cell(row, 3).Value = item.NgayLap; worksheet.Cell(row, 4).Value = item.TongTien;
                            worksheet.Cell(row, 5).Value = item.TrangThai; worksheet.Cell(row, 6).Value = item.LoaiHD;
                            row++;
                        }
                        workbook.SaveAs(saveFileDialog.FileName);
                    }
                    MessageBox.Show("Xuất Excel thành công!");
                }
                catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
            }
        }

        // --- CÁC HÀM UI HELPER ---
        private void InitFilterData()
        {
            cbbMonth.Items.Clear(); cbbMonth.Items.Add("Tất cả"); for (int i = 1; i <= 12; i++) cbbMonth.Items.Add(i.ToString()); cbbMonth.SelectedIndex = 0;
            cbbYear.Items.Clear(); cbbYear.Items.Add("Tất cả"); int cy = DateTime.Now.Year; for (int i = cy; i >= 1900; i--) cbbYear.Items.Add(i.ToString()); cbbYear.SelectedIndex = 0;
            UpdateStatusComboBox();
        }
        private void UpdateStatusComboBox() { cbbStatus.Items.Clear(); cbbStatus.Items.Add(new ComboBoxItem { Content = "Tất cả", Tag = "All", IsSelected = true }); cbbStatus.Items.Add(CreateColorItem("Đã thanh toán", "#38A169")); cbbStatus.Items.Add(CreateColorItem("Chờ duyệt", "#DD6B20")); }
        private ComboBoxItem CreateColorItem(string text, string hex) { return new ComboBoxItem { Content = text, Tag = text }; }
        private void UpdateMaxValue(double? maxVal) { _maxInvoiceValue = (decimal)(maxVal ?? 100000000); if (sldPrice != null) { sldPrice.Maximum = (double)_maxInvoiceValue; sldPrice.Value = (double)_maxInvoiceValue; } if (txtSliderValue != null) txtSliderValue.Text = _maxInvoiceValue.ToString("N0"); }
        private void Tab_Click(object sender, RoutedEventArgs e) { var btn = sender as Button; if (btn == null || btn.Tag.ToString() == _currentTab) return; _currentTab = btn.Tag.ToString(); UpdateTabVisuals(); LoadDataFromDatabase(true); }
        private void UpdateTabVisuals() { var act = (Brush)new BrushConverter().ConvertFrom("#4C70BA"); var inact = Brushes.White; if (_currentTab == "Xuat") { btnTabXuat.Background = act; btnTabXuat.Foreground = Brushes.White; btnTabNhap.Background = inact; btnTabNhap.Foreground = act; } else { btnTabNhap.Background = act; btnTabNhap.Foreground = Brushes.White; btnTabXuat.Background = inact; btnTabXuat.Foreground = act; } }
        private void cbbSortCriteria_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateSortIcon(); LoadDataFromDatabase(false); }
        private void btnSortDirection_Click(object sender, RoutedEventArgs e) { _isSortAscending = !_isSortAscending; UpdateSortIcon(); LoadDataFromDatabase(false); }
        private void UpdateSortIcon() { if (iconSort != null) iconSort.Kind = _isSortAscending ? PackIconKind.SortNumericAscending : PackIconKind.SortNumericDescending; }
        private void sldPrice_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (!_isSyncing && txtSliderValue != null) { _isSyncing = true; txtSliderValue.Text = e.NewValue.ToString("N0"); _isSyncing = false; } }
        private void txtSliderValue_TextChanged(object sender, TextChangedEventArgs e) { /* Logic slider text */ }
        private void btnApplyFilter_Click(object sender, RoutedEventArgs e) => LoadDataFromDatabase(false);
        private void btnResetFilter_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; cbbStatus.SelectedIndex = 0; LoadDataFromDatabase(false); }
        private void btnToggleFilter_Click(object sender, RoutedEventArgs e) => FilterPanel.Visibility = FilterPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadDataFromDatabase(false);
        private void btnPrint_Click(object sender, RoutedEventArgs e) { MessageBox.Show("In thành công"); }
        private void btnAddNew_Click(object sender, RoutedEventArgs e) { if (new AddInvoiceWindow().ShowDialog() == true) { LoadDataFromDatabase(); LoadNotifications(); } }
        private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (!IsUserClickingOnRow(e)) dgHoaDon.UnselectAll(); }
        private bool IsUserClickingOnRow(MouseButtonEventArgs e) { return false; /* Simple implementation */ }
        private void dgHoaDon_PreviewKeyDown(object sender, KeyEventArgs e) { }
        private void dgHoaDon_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (dgHoaDon.SelectedItem is InvoiceViewModel item) new ChiTietHoaDonWindow(item).ShowDialog(); }
    }
}