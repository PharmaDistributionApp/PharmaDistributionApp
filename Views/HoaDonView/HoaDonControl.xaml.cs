using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace PharmaDistributionApp.Views.Controls
{
    public partial class HoaDonControl : UserControl
    {
        // --- BIẾN TOÀN CỤC ---
        private string _currentTab = "Xuat";
        private decimal _maxInvoiceValue = 100000000;
        private bool _isSyncing = false;
        private bool _isSortAscending = false;

        // Danh sách chức vụ được quyền duyệt
        private readonly string[] _approverRoles = { "Admin", "Quản lý", "Giám đốc", "Kế toán", "ADMIN", "admin" };

        public HoaDonControl()
        {
            InitializeComponent();

            // [QUAN TRỌNG - ĐÃ SỬA LỖI CHO EPPLUS 8.x]
            // Phải dùng ExcelPackage.License.LicenseContext thay vì ExcelPackage.LicenseContext
            

            InitFilterData();

            this.Loaded += (s, e) =>
            {
                LoadDataFromDatabase(true);
                LoadNotifications();
            };

            UpdateTabVisuals();
        }

        // ===================================================================
        // PHẦN 1: LOGIC THÔNG BÁO & PHÊ DUYỆT
        // ===================================================================
        private void LoadNotifications()
        {
            if (!UserSession.IsLoggedIn || UserSession.CurrentUser == null)
            {
                gridNotification.Visibility = Visibility.Collapsed;
                return;
            }

            string currentRole = UserSession.CurrentUser.Chucvu?.Trim();
            bool isBoss = _approverRoles.Any(r => r.Equals(currentRole, StringComparison.OrdinalIgnoreCase));

            if (!isBoss)
            {
                gridNotification.Visibility = Visibility.Collapsed;
                return;
            }

            gridNotification.Visibility = Visibility.Visible;

            try
            {
                string sql = @"
                    SELECT SOHDNHAP AS Ma, (SELECT TENNCC FROM NHACUNGCAP WHERE MANCC = H.MANCC) AS DoiTac, 
                           NGAYLAP, TONGTIEN, 
                           CASE WHEN TRANGTHAI = 'Yêu cầu xóa' THEN 'Yêu cầu XÓA' ELSE 'Nhập' END AS Loai 
                    FROM HOADONNHAP H 
                    WHERE TRANGTHAI IN ('Chờ duyệt', 'Yêu cầu xóa')
                    
                    UNION ALL
                    
                    SELECT SOHDXUAT AS Ma, (SELECT TENKH FROM KHACHHANG WHERE MAKH = H.MAKH) AS DoiTac, 
                           NGAYLAP, TONGTIEN, 
                           CASE WHEN TRANGTHAI = 'Yêu cầu xóa' THEN 'Yêu cầu XÓA' ELSE 'Xuất' END AS Loai 
                    FROM HOADONXUAT H 
                    WHERE TRANGTHAI IN ('Chờ duyệt', 'Yêu cầu xóa')
                    
                    ORDER BY NGAYLAP DESC";

                var dt = Database.GetTable(sql);
                var listPending = new List<InvoiceViewModel>();

                foreach (DataRow r in dt.Rows)
                {
                    listPending.Add(new InvoiceViewModel
                    {
                        MaHD = r["Ma"].ToString(),
                        DoiTac = r["DoiTac"].ToString(),
                        NgayLap = DateTime.Parse(r["NGAYLAP"].ToString()),
                        TongTien = Convert.ToDecimal(r["TONGTIEN"]),
                        TrangThai = r["Loai"].ToString() == "Yêu cầu XÓA" ? "Yêu cầu xóa" : "Chờ duyệt",
                        LoaiHD = r["Loai"].ToString()
                    });
                }

                if (listPending.Count > 0)
                {
                    bdBadge.Visibility = Visibility.Visible;
                    txtBadgeCount.Text = listPending.Count.ToString();
                    lvPendingInvoices.ItemsSource = listPending;
                }
                else
                {
                    bdBadge.Visibility = Visibility.Collapsed;
                    lvPendingInvoices.ItemsSource = null;
                }
            }
            catch { }
        }

        private void lvPendingInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvPendingInvoices.SelectedItem is InvoiceViewModel selectedInv)
            {
                if (btnNoti != null) btnNoti.IsChecked = false; // Đóng popup

                var detailWindow = new ChiTietHoaDonWindow(selectedInv);
                detailWindow.ShowDialog();

                LoadDataFromDatabase(false);
                LoadNotifications();
            }
        }

        // ===================================================================
        // PHẦN 2: XUẤT EXCEL (EPPlus 8.x)
        // ===================================================================
        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var listData = dgHoaDon.ItemsSource as List<InvoiceViewModel>;
                if (listData == null || listData.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo");
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    FileName = $"DS_HoaDon_{DateTime.Now:ddMMyyyy_HHmm}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Danh sách hóa đơn");

                        // Header
                        string[] headers = { "Mã HĐ", "Đối tác", "Ngày lập", "Tổng tiền", "Trạng thái", "Loại HĐ" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = headers[i];
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                            worksheet.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            worksheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }

                        // Data
                        int row = 2;
                        foreach (var item in listData)
                        {
                            worksheet.Cells[row, 1].Value = item.MaHD;
                            worksheet.Cells[row, 2].Value = item.DoiTac;
                            worksheet.Cells[row, 3].Value = item.NgayLap.ToString("dd/MM/yyyy");

                            worksheet.Cells[row, 4].Value = item.TongTien;
                            worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";

                            worksheet.Cells[row, 5].Value = item.TrangThai;
                            worksheet.Cells[row, 6].Value = item.LoaiHD;

                            // Kẻ khung
                            for (int i = 1; i <= 6; i++)
                            {
                                worksheet.Cells[row, i].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            }
                            row++;
                        }

                        worksheet.Cells.AutoFitColumns();

                        // Lưu file
                        File.WriteAllBytes(saveFileDialog.FileName, package.GetAsByteArray());
                        MessageBox.Show("Xuất Excel thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất Excel: " + ex.Message);
            }
        }

        // ===================================================================
        // PHẦN 3: SỬA & XÓA
        // ===================================================================
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvoiceViewModel item)
            {
                if (item.TrangThai == "Chờ duyệt" || item.TrangThai == "Yêu cầu xóa")
                {
                    MessageBox.Show("Hóa đơn này đang đợi duyệt, không thể chỉnh sửa.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var editWindow = new EditInvoiceWindow(item);
                editWindow.ShowDialog();

                LoadDataFromDatabase(false);
                LoadNotifications();
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvoiceViewModel item)
            {
                bool isBoss = UserSession.CurrentUser.Chucvu == "Admin";

                if (item.TrangThai == "Yêu cầu xóa")
                {
                    MessageBox.Show("Đã gửi yêu cầu xóa rồi. Vui lòng chờ Admin duyệt.", "Thông báo");
                    return;
                }

                string msg = isBoss ? $"Bạn là Admin. XÓA VĨNH VIỄN hóa đơn {item.MaHD}?" : $"Gửi yêu cầu XÓA hóa đơn {item.MaHD} cho Admin?";

                if (MessageBox.Show(msg, "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (isBoss)
                    {
                        UpdateStatus(item, "Đã hủy");
                        MessageBox.Show("Đã hủy hóa đơn thành công.");
                    }
                    else
                    {
                        UpdateStatus(item, "Yêu cầu xóa");
                        MessageBox.Show("Đã gửi yêu cầu xóa thành công.");
                    }
                    LoadDataFromDatabase(false);
                    LoadNotifications();
                }
            }
        }

        private void UpdateStatus(InvoiceViewModel item, string status)
        {
            string table = item.LoaiHD == "Xuất" ? "HOADONXUAT" : "HOADONNHAP";
            string colID = item.LoaiHD == "Xuất" ? "SOHDXUAT" : "SOHDNHAP";
            string sql = $"UPDATE {table} SET TRANGTHAI = '{status}' WHERE {colID} = '{item.MaHD}'";

            using (var conn = new System.Data.SQLite.SQLiteConnection("Data Source=PharmaDB.db"))
            {
                conn.Open();
                new System.Data.SQLite.SQLiteCommand(sql, conn).ExecuteNonQuery();
            }
        }

        // ===================================================================
        // PHẦN 4: TẢI DỮ LIỆU CHÍNH
        // ===================================================================
        private void LoadDataFromDatabase(bool updateSliderMax = false)
        {
            if (dgHoaDon == null) return;
            try
            {
                string sql = "";

                if (_currentTab == "Xuat")
                {
                    sql = @"
                        SELECT H.SOHDXUAT AS MaHD, 
                               IFNULL(K.TENKH, 'Khách lẻ') AS DoiTac, 
                               H.NGAYLAP, 
                               H.TONGTIEN AS TienHang, 
                               IFNULL(H.VAT, 0) AS VAT,
                               H.TRANGTHAI
                        FROM HOADONXUAT H
                        LEFT JOIN KHACHHANG K ON H.MAKH = K.MAKH
                        WHERE H.TRANGTHAI NOT IN ('Chờ duyệt', 'Yêu cầu xóa')";
                }
                else
                {
                    sql = @"
                        SELECT H.SOHDNHAP AS MaHD, 
                               IFNULL(N.TENNCC, 'NCC Vãng lai') AS DoiTac, 
                               H.NGAYLAP, 
                               H.TONGTIEN AS TienHang, 
                               IFNULL(H.VAT, 0) AS VAT,
                               H.TRANGTHAI
                        FROM HOADONNHAP H
                        LEFT JOIN NHACUNGCAP N ON H.MANCC = N.MANCC
                        WHERE H.TRANGTHAI NOT IN ('Chờ duyệt', 'Yêu cầu xóa')";
                }

                var dt = Database.GetTable(sql);
                var displayList = new List<InvoiceViewModel>();

                foreach (DataRow r in dt.Rows)
                {
                    decimal tienHang = Convert.ToDecimal(r["TienHang"]);
                    decimal vatRate = Convert.ToDecimal(r["VAT"]);
                    decimal tongCong = tienHang + (tienHang * vatRate / 100m);

                    displayList.Add(new InvoiceViewModel
                    {
                        MaHD = r["MaHD"].ToString(),
                        DoiTac = r["DoiTac"].ToString(),
                        NgayLap = DateTime.Parse(r["NGAYLAP"].ToString()),
                        TongTien = tongCong,
                        TrangThai = r["TRANGTHAI"].ToString(),
                        LoaiHD = _currentTab == "Xuat" ? "Xuất" : "Nhập"
                    });
                }

                if (updateSliderMax && displayList.Any())
                {
                    UpdateMaxValue((double)displayList.Max(x => x.TongTien));
                }

                // --- BỘ LỌC ---
                if (txtSearch != null && !string.IsNullOrEmpty(txtSearch.Text))
                {
                    string k = txtSearch.Text.ToLower();
                    displayList = displayList.Where(x => x.MaHD.ToLower().Contains(k) || x.DoiTac.ToLower().Contains(k)).ToList();
                }
                if (cbbMonth.SelectedIndex > 0)
                    displayList = displayList.Where(x => x.NgayLap.Month == int.Parse(cbbMonth.SelectedItem.ToString())).ToList();
                if (cbbYear.SelectedIndex > 0)
                    displayList = displayList.Where(x => x.NgayLap.Year == int.Parse(cbbYear.SelectedItem.ToString())).ToList();
                if (cbbStatus.SelectedItem is ComboBoxItem selectedItem)
                {
                    string status = selectedItem.Tag?.ToString();
                    if (status != "All" && !string.IsNullOrEmpty(status))
                        displayList = displayList.Where(x => x.TrangThai == status).ToList();
                }
                if (sldPrice != null && sldPrice.Value < (double)_maxInvoiceValue)
                    displayList = displayList.Where(x => x.TongTien <= (decimal)sldPrice.Value).ToList();

                // Sắp xếp
                string sortType = (cbbSortCriteria.SelectedItem as ComboBoxItem)?.Tag.ToString();
                if (sortType == "TongTien")
                    displayList = _isSortAscending ? displayList.OrderBy(x => x.TongTien).ToList() : displayList.OrderByDescending(x => x.TongTien).ToList();
                else if (sortType == "Ten")
                    displayList = _isSortAscending ? displayList.OrderBy(x => x.DoiTac).ToList() : displayList.OrderByDescending(x => x.DoiTac).ToList();
                else
                    displayList = _isSortAscending ? displayList.OrderBy(x => x.NgayLap).ToList() : displayList.OrderByDescending(x => x.NgayLap).ToList();

                dgHoaDon.ItemsSource = displayList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        // ===================================================================
        // PHẦN 5: CÁC SỰ KIỆN GIAO DIỆN & HELPER
        // ===================================================================

        private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsUserClickingOnRow(e)) { dgHoaDon.UnselectAll(); Keyboard.ClearFocus(); }
        }

        private bool IsUserClickingOnRow(MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridRow) && !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            return dep is DataGridRow || dep is DataGridColumnHeader;
        }

        private void dgHoaDon_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { e.Handled = true; OpenDetailWindow(); }
        }

        private void dgHoaDon_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (IsUserClickingOnRow(e)) OpenDetailWindow(); }
        private void OpenDetailWindow() { if (dgHoaDon.SelectedItem is InvoiceViewModel item) { new ChiTietHoaDonWindow(item).ShowDialog(); LoadDataFromDatabase(false); } }

        private void InitFilterData()
        {
            cbbMonth.Items.Clear(); cbbMonth.Items.Add("Tất cả"); for (int i = 1; i <= 12; i++) cbbMonth.Items.Add(i.ToString()); cbbMonth.SelectedIndex = 0;
            cbbYear.Items.Clear(); cbbYear.Items.Add("Tất cả"); int currentYear = DateTime.Now.Year;

            // Thay đổi vòng lặp để chạy từ năm hiện tại lùi về năm 1900
            for (int i = currentYear; i >= 1900; i--)
            {
                cbbYear.Items.Add(i.ToString());
            }

            cbbYear.SelectedIndex = 0;

            UpdateStatusComboBox();
        }

        private void UpdateStatusComboBox()
        {
            cbbStatus.Items.Clear();
            cbbStatus.Items.Add(new ComboBoxItem { Content = "Tất cả", Tag = "All", IsSelected = true });
            cbbStatus.Items.Add(CreateColorItem("Đã thanh toán", "#38A169"));
            cbbStatus.Items.Add(CreateColorItem("Chờ thanh toán", "#DD6B20"));
            cbbStatus.Items.Add(CreateColorItem("Đã hủy", "#E53E3E"));
        }

        private ComboBoxItem CreateColorItem(string text, string hexColor)
        {
            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal };
            Ellipse dot = new Ellipse { Width = 10, Height = 10, Fill = (Brush)new BrushConverter().ConvertFrom(hexColor), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            TextBlock tb = new TextBlock { Text = text, Foreground = (Brush)new BrushConverter().ConvertFrom(hexColor), FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
            panel.Children.Add(dot); panel.Children.Add(tb); return new ComboBoxItem { Content = panel, Tag = text };
        }

        private void UpdateMaxValue(double? maxVal)
        {
            _maxInvoiceValue = (decimal)(maxVal ?? 100000000);
            if (sldPrice != null) { sldPrice.Maximum = (double)_maxInvoiceValue; sldPrice.Value = (double)_maxInvoiceValue; }
            if (txtSliderValue != null) { _isSyncing = true; txtSliderValue.Text = _maxInvoiceValue.ToString("N0"); _isSyncing = false; }
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button; if (btn == null || btn.Tag.ToString() == _currentTab) return;
            _currentTab = btn.Tag.ToString(); UpdateTabVisuals(); UpdateStatusComboBox(); LoadDataFromDatabase(true);
        }

        private void UpdateTabVisuals()
        {
            var activeBg = (Brush)new BrushConverter().ConvertFrom("#4C70BA"); var activeFg = Brushes.White;
            var inactiveBg = Brushes.White; var inactiveFg = (Brush)new BrushConverter().ConvertFrom("#4C70BA");
            if (_currentTab == "Xuat") { btnTabXuat.Background = activeBg; btnTabXuat.Foreground = activeFg; btnTabNhap.Background = inactiveBg; btnTabNhap.Foreground = inactiveFg; }
            else { btnTabNhap.Background = activeBg; btnTabNhap.Foreground = activeFg; btnTabXuat.Background = inactiveBg; btnTabXuat.Foreground = inactiveFg; }
        }

        private void cbbSortCriteria_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateSortIcon(); LoadDataFromDatabase(false); }
        private void btnSortDirection_Click(object sender, RoutedEventArgs e) { _isSortAscending = !_isSortAscending; UpdateSortIcon(); LoadDataFromDatabase(false); }

        private void UpdateSortIcon()
        {
            if (iconSort == null || cbbSortCriteria == null) return;
            string criteria = (cbbSortCriteria.SelectedItem as ComboBoxItem)?.Tag.ToString();
            if (criteria == "TongTien") iconSort.Kind = _isSortAscending ? PackIconKind.SortNumericAscending : PackIconKind.SortNumericDescending;
            else if (criteria == "Ten") iconSort.Kind = _isSortAscending ? PackIconKind.SortAlphabeticalAscending : PackIconKind.SortAlphabeticalDescending;
            else iconSort.Kind = _isSortAscending ? PackIconKind.SortCalendarAscending : PackIconKind.SortCalendarDescending;
        }

        private void sldPrice_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (_isSyncing || txtSliderValue == null) return; _isSyncing = true; txtSliderValue.Text = e.NewValue.ToString("N0"); _isSyncing = false; }

        private void txtSliderValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing || sldPrice == null) return; _isSyncing = true; try
            {
                string rawText = txtSliderValue.Text.Replace(",", "").Replace(".", "").Trim();
                if (double.TryParse(rawText, out double value)) { if (value > sldPrice.Maximum) sldPrice.Value = sldPrice.Maximum; else sldPrice.Value = value; txtSliderValue.Text = value.ToString("N0"); txtSliderValue.CaretIndex = txtSliderValue.Text.Length; } else if (string.IsNullOrEmpty(rawText)) sldPrice.Value = 0;
            }
            catch { }
            _isSyncing = false;
        }

        private void btnApplyFilter_Click(object sender, RoutedEventArgs e) => LoadDataFromDatabase(false);
        private void btnResetFilter_Click(object sender, RoutedEventArgs e) { cbbMonth.SelectedIndex = 0; cbbYear.SelectedIndex = 0; if (cbbStatus.Items.Count > 0) cbbStatus.SelectedIndex = 0; if (sldPrice != null) sldPrice.Value = sldPrice.Maximum; txtSearch.Text = ""; LoadDataFromDatabase(false); }
        private void btnToggleFilter_Click(object sender, RoutedEventArgs e) => FilterPanel.Visibility = (FilterPanel.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadDataFromDatabase(false);
        private void btnPrint_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.Tag is InvoiceViewModel item) MessageBox.Show($"In hóa đơn: {item.MaHD}", "In ấn", MessageBoxButton.OK, MessageBoxImage.Information); }
        private void btnAddNew_Click(object sender, RoutedEventArgs e) { var addWindow = new AddInvoiceWindow(); addWindow.ShowDialog(); LoadDataFromDatabase(false); LoadNotifications(); }
    }
}