using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace PharmaDistributionApp.Views.Controls
{

    public class StatusToColorConverter : IValueConverter
    {

        private Brush GetBrush(string hex)
        {
            return (Brush)new BrushConverter().ConvertFrom(hex);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            if (string.IsNullOrEmpty(status)) return GetBrush("#2C3E50");

            status = status.Trim();

            if (status.Equals("Đã thanh toán", StringComparison.OrdinalIgnoreCase))
                return GetBrush("#27AE60"); 

            if (status.Equals("Chờ thanh toán", StringComparison.OrdinalIgnoreCase) || status.Equals("Công nợ", StringComparison.OrdinalIgnoreCase))
                return GetBrush("#F39C12"); 

            if (status.Equals("Chờ duyệt", StringComparison.OrdinalIgnoreCase))
                return GetBrush("#2980B9"); 
            if (status.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase))
                return GetBrush("#C0392B"); 

            return GetBrush("#7F8C8D"); 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public partial class HoaDonControl : UserControl
    {
        private string _currentTab = "Xuat";
        private decimal _maxInvoiceValue = 100000000;
        private bool _isSyncing = false;
        private bool _isSortAscending = false;
        private readonly string[] _approverRoles = { "Admin", "Quản lý", "Giám đốc", "Kế toán", "ADMIN", "QuanLy", "TruongKho" };

        public static readonly DependencyProperty UserActionVisibilityProperty =
            DependencyProperty.Register("UserActionVisibility", typeof(Visibility), typeof(HoaDonControl), new PropertyMetadata(Visibility.Collapsed));

        public Visibility UserActionVisibility
        {
            get { return (Visibility)GetValue(UserActionVisibilityProperty); }
            set { SetValue(UserActionVisibilityProperty, value); }
        }
        public HoaDonControl()
        {
            InitializeComponent();
            EnsureTableStructure();
            InitFilterData();

            this.Loaded += (s, e) =>
            {
                if (IsBoss())
                {
                    UserActionVisibility = Visibility.Visible;
                }
                else
                {
                    UserActionVisibility = Visibility.Collapsed;
                }

                btnAddNew.Visibility = Visibility.Visible;

                LoadDataFromDatabase(true);
                LoadNotifications();
            };
            UpdateTabVisuals();
        }

        private void EnsureTableStructure()
        {
            try { using (var conn = new SQLiteConnection(Database.ConnectionString)) { conn.Open(); try { new SQLiteCommand("ALTER TABLE HOADONXUAT ADD COLUMN PheDuyet INTEGER DEFAULT 0", conn).ExecuteNonQuery(); } catch { } try { new SQLiteCommand("ALTER TABLE HOADONNHAP ADD COLUMN PheDuyet INTEGER DEFAULT 0", conn).ExecuteNonQuery(); } catch { } } } catch { }
        }

        private void InitFilterData()
        {
            cbbMonth.Items.Clear(); cbbMonth.Items.Add("Tất cả"); for (int i = 1; i <= 12; i++) cbbMonth.Items.Add(i.ToString()); cbbMonth.SelectedIndex = 0;
            cbbYear.Items.Clear(); cbbYear.Items.Add("Tất cả"); int cy = DateTime.Now.Year; for (int i = cy; i >= 1900; i--) cbbYear.Items.Add(i.ToString()); cbbYear.SelectedIndex = 0;

            UpdateStatusComboBox();
        }

        private void UpdateStatusComboBox()
        {
            cbbStatus.Items.Clear();

            cbbStatus.Items.Add(CreateBadgeItem("Tất cả", "All", "#F5F5F5", "#666666"));

            cbbStatus.Items.Add(CreateBadgeItem("Đã thanh toán", "Đã thanh toán", "#F0FFF4", "#2F855A"));

            cbbStatus.Items.Add(CreateBadgeItem("Chờ thanh toán", "Chờ thanh toán", "#FFF9E5", "#D97706"));

            cbbStatus.Items.Add(CreateBadgeItem("Chờ duyệt", "Chờ duyệt", "#E3F2FD", "#1565C0"));

            cbbStatus.Items.Add(CreateBadgeItem("Đã hủy", "Đã hủy", "#FFF5F5", "#C53030"));

            cbbStatus.SelectedIndex = 0;
        }
        private ComboBoxItem CreateBadgeItem(string text, string tagValue, string bgHex, string fgHex)
        {
            var converter = new BrushConverter();

            var border = new Border
            {
                CornerRadius = new CornerRadius(12),       
                Padding = new Thickness(12, 5, 12, 5),     
                Margin = new Thickness(0, 2, 0, 2),        
                Background = (Brush)converter.ConvertFrom(bgHex), 
                HorizontalAlignment = HorizontalAlignment.Left    
            };

            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = (Brush)converter.ConvertFrom(fgHex), 
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = textBlock;

            return new ComboBoxItem
            {
                Content = border,
                Tag = tagValue 
            };
        }

        private ComboBoxItem CreateColoredItem(string text, Brush color)
        {
            return new ComboBoxItem
            {
                Tag = text,
                Content = new TextBlock { Text = text, Foreground = color, FontWeight = FontWeights.SemiBold }
            };
        }


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

                string sql = $@"SELECT H.{colID} AS MaHD, IFNULL(P.{namePartner}, 'Khách lẻ/Vãng lai') AS DoiTac, H.NGAYLAP, H.TONGTIEN, H.TRANGTHAI, H.PheDuyet FROM {table} H LEFT JOIN {partner} P ON H.{colPartner} = P.{colPartner}";

                var dt = Database.GetTable(sql);
                var list = new List<InvoiceViewModel>();

                foreach (DataRow r in dt.Rows)
                {
                    int pheDuyet = Convert.ToInt32(r["PheDuyet"]);
                    string displayStatus = pheDuyet == 1 ? "Chờ duyệt" : r["TRANGTHAI"].ToString();

                    list.Add(new InvoiceViewModel
                    {
                        MaHD = r["MaHD"].ToString(),
                        DoiTac = r["DoiTac"].ToString(),
                        NgayLap = DateTime.Parse(r["NGAYLAP"].ToString()),
                        TongTien = Convert.ToDecimal(r["TONGTIEN"]),
                        TrangThai = displayStatus,
                        PheDuyet = pheDuyet,
                        LoaiHD = _currentTab == "Xuat" ? "Xuất" : "Nhập"
                    });
                }
                if (updateSlider && list.Any()) UpdateMaxValue((double)list.Max(x => x.TongTien));

                var filtered = list.AsEnumerable();
                if (!string.IsNullOrEmpty(txtSearch.Text)) { string k = txtSearch.Text.ToLower(); filtered = filtered.Where(x => x.MaHD.ToLower().Contains(k) || x.DoiTac.ToLower().Contains(k)); }
                if (cbbStatus.SelectedItem is ComboBoxItem item && item.Tag?.ToString() != "All") filtered = filtered.Where(x => x.TrangThai == item.Tag.ToString());
                if (sldPrice != null && sldPrice.Value < (double)_maxInvoiceValue) filtered = filtered.Where(x => x.TongTien <= (decimal)sldPrice.Value);
                if (cbbMonth != null && cbbMonth.SelectedIndex > 0) 
                {
                    string monthStr = cbbMonth.SelectedItem.ToString();
                    if (int.TryParse(monthStr, out int month))
                    {
                        filtered = filtered.Where(x => x.NgayLap.Month == month);
                    }
                }


                if (cbbYear != null && cbbYear.SelectedIndex > 0) 
                {
                    string yearStr = cbbYear.SelectedItem.ToString();
                    if (int.TryParse(yearStr, out int year))
                    {
                        filtered = filtered.Where(x => x.NgayLap.Year == year);
                    }
                }
                string sortType = (cbbSortCriteria.SelectedItem as ComboBoxItem)?.Tag.ToString();
                if (sortType == "TongTien") filtered = _isSortAscending ? filtered.OrderBy(x => x.TongTien) : filtered.OrderByDescending(x => x.TongTien);
                else filtered = _isSortAscending ? filtered.OrderBy(x => x.NgayLap) : filtered.OrderByDescending(x => x.NgayLap);

                dgHoaDon.ItemsSource = filtered.ToList();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message); }
        }

        private void UpdateMaxValue(double? maxVal) { _maxInvoiceValue = (decimal)(maxVal ?? 100000000); if (sldPrice != null) { sldPrice.Maximum = (double)_maxInvoiceValue; sldPrice.Value = (double)_maxInvoiceValue; } if (txtSliderValue != null) txtSliderValue.Text = _maxInvoiceValue.ToString("N0"); }
        private void Tab_Click(object sender, RoutedEventArgs e) { var btn = sender as Button; if (btn == null || btn.Tag.ToString() == _currentTab) return; _currentTab = btn.Tag.ToString(); UpdateTabVisuals(); LoadDataFromDatabase(true); }
        private void UpdateTabVisuals() { var act = (Brush)new BrushConverter().ConvertFrom("#4C70BA"); var inact = Brushes.White; if (_currentTab == "Xuat") { btnTabXuat.Background = act; btnTabXuat.Foreground = Brushes.White; btnTabNhap.Background = inact; btnTabNhap.Foreground = act; } else { btnTabNhap.Background = act; btnTabNhap.Foreground = Brushes.White; btnTabXuat.Background = inact; btnTabXuat.Foreground = act; } }
        private void cbbSortCriteria_SelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateSortIcon(); LoadDataFromDatabase(false); }
        private void btnSortDirection_Click(object sender, RoutedEventArgs e) { _isSortAscending = !_isSortAscending; UpdateSortIcon(); LoadDataFromDatabase(false); }
        private void UpdateSortIcon() { if (iconSort != null) iconSort.Kind = _isSortAscending ? PackIconKind.SortNumericAscending : PackIconKind.SortNumericDescending; }
        private void sldPrice_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (!_isSyncing && txtSliderValue != null) { _isSyncing = true; txtSliderValue.Text = e.NewValue.ToString("N0"); _isSyncing = false; } }
        private void txtSliderValue_TextChanged(object sender, TextChangedEventArgs e) { }
        private void btnApplyFilter_Click(object sender, RoutedEventArgs e) => LoadDataFromDatabase(false);
        private void btnResetFilter_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; cbbStatus.SelectedIndex = 0; if (sldPrice != null) sldPrice.Value = sldPrice.Maximum; LoadDataFromDatabase(false); }
        private void btnToggleFilter_Click(object sender, RoutedEventArgs e) => FilterPanel.Visibility = FilterPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadDataFromDatabase(false);
        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvoiceViewModel item)
            {
                ExportInvoiceToPDF(item);
            }
        }


        private void ExportInvoiceToPDF(InvoiceViewModel invoice)
        {
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                SaveFileDialog dlg = new SaveFileDialog
                {
                    Filter = "PDF File (*.pdf)|*.pdf",
                    FileName = $"HoaDon_{invoice.MaHD}_{DateTime.Now:ddMMyy}.pdf"
                };

                if (dlg.ShowDialog() == true)
                {
                    bool isExport = invoice.LoaiHD == "Xuất" || invoice.MaHD.StartsWith("HDX");
                    string tblCT = isExport ? "CTHDXUAT" : "CTHDNHAP";
                    string colID = isExport ? "SOHDXUAT" : "SOHDNHAP";
                    string colGia = isExport ? "DONGIABAN" : "DONGIANHAP";

                    string sql = $@"
                        SELECT SP.TENSP, SP.DVT, CT.SOLUONG, CT.{colGia} AS DONGIA, CT.THANHTIEN 
                        FROM {tblCT} CT 
                        JOIN SANPHAM SP ON CT.MASP = SP.MASP 
                        WHERE CT.{colID} = '{invoice.MaHD}'";

                    DataTable dtDetails = Database.GetTable(sql);

                    Document doc = new Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(dlg.FileName, FileMode.Create));

                    doc.Open();
                    string fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    if (!File.Exists(fontPath))
                    {
                        fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "tahoma.ttf");
                    }

                    BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

                    Font fTitle = new Font(bf, 20, Font.BOLD, BaseColor.BLUE);
                    Font fHeader = new Font(bf, 12, Font.BOLD, BaseColor.BLACK);
                    Font fNormal = new Font(bf, 11, Font.NORMAL, BaseColor.BLACK);
                    Font fItalic = new Font(bf, 10, Font.ITALIC, BaseColor.GRAY);
                    Font fTotal = new Font(bf, 14, Font.BOLD, BaseColor.RED);

                    string titleText = isExport ? "HÓA ĐƠN BÁN HÀNG" : "HÓA ĐƠN NHẬP KHO";
                    Paragraph pTitle = new Paragraph(titleText, fTitle);
                    pTitle.Alignment = Element.ALIGN_CENTER;
                    pTitle.SpacingAfter = 20;
                    doc.Add(pTitle);

                    PdfPTable tInfo = new PdfPTable(2);
                    tInfo.WidthPercentage = 100;
                    tInfo.SetWidths(new float[] { 1f, 1f });

                    PdfPCell cLeft = new PdfPCell();
                    cLeft.Border = Rectangle.NO_BORDER;
                    cLeft.AddElement(new Paragraph($"Đối tác: {invoice.DoiTac}", fHeader));
                    cLeft.AddElement(new Paragraph($"Loại: {invoice.LoaiHD}", fNormal));
                    tInfo.AddCell(cLeft);

                    PdfPCell cRight = new PdfPCell();
                    cRight.Border = Rectangle.NO_BORDER;
                    cRight.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cRight.AddElement(new Paragraph($"Số HĐ: {invoice.MaHD}", fHeader));
                    cRight.AddElement(new Paragraph($"Ngày: {invoice.NgayLap:dd/MM/yyyy HH:mm}", fNormal));
                    cRight.AddElement(new Paragraph($"Trạng thái: {invoice.TrangThai}", fItalic));
                    tInfo.AddCell(cRight);

                    doc.Add(tInfo);
                    doc.Add(new Paragraph(" ", fNormal));

                    PdfPTable tData = new PdfPTable(5);
                    tData.WidthPercentage = 100;
                    tData.SetWidths(new float[] { 10f, 40f, 15f, 15f, 20f });
                    tData.SpacingBefore = 10;
                    tData.SpacingAfter = 10;

                    AddCellToTable(tData, "STT", fHeader, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
                    AddCellToTable(tData, "Tên Sản Phẩm", fHeader, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
                    AddCellToTable(tData, "ĐVT", fHeader, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
                    AddCellToTable(tData, "SL", fHeader, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);
                    AddCellToTable(tData, "Thành Tiền", fHeader, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER);

                    int stt = 1;
                    foreach (DataRow row in dtDetails.Rows)
                    {
                        AddCellToTable(tData, stt++.ToString(), fNormal, null, Element.ALIGN_CENTER);
                        AddCellToTable(tData, row["TENSP"].ToString(), fNormal, null, Element.ALIGN_LEFT);
                        AddCellToTable(tData, row["DVT"].ToString(), fNormal, null, Element.ALIGN_CENTER);
                        AddCellToTable(tData, Convert.ToInt32(row["SOLUONG"]).ToString("N0"), fNormal, null, Element.ALIGN_CENTER);
                        AddCellToTable(tData, Convert.ToDecimal(row["THANHTIEN"]).ToString("N0"), fNormal, null, Element.ALIGN_RIGHT);
                    }

                    doc.Add(tData);

                    Paragraph pTotal = new Paragraph($"Tổng thanh toán: {invoice.TongTien:N0} VND", fTotal);
                    pTotal.Alignment = Element.ALIGN_RIGHT;
                    doc.Add(pTotal);

                    PdfPTable tSign = new PdfPTable(2);
                    tSign.WidthPercentage = 100;
                    tSign.SpacingBefore = 30;

                    PdfPCell cSign1 = new PdfPCell(new Phrase("Người lập phiếu", fHeader));
                    cSign1.Border = Rectangle.NO_BORDER;
                    cSign1.HorizontalAlignment = Element.ALIGN_CENTER;

                    PdfPCell cSign2 = new PdfPCell(new Phrase(isExport ? "Khách hàng" : "Nhà cung cấp", fHeader));
                    cSign2.Border = Rectangle.NO_BORDER;
                    cSign2.HorizontalAlignment = Element.ALIGN_CENTER;

                    tSign.AddCell(cSign1);
                    tSign.AddCell(cSign2);
                    doc.Add(tSign);

                    doc.Close();
                    writer.Close();

                    if (MessageBox.Show("Xuất PDF thành công! Bạn có muốn mở file ngay không?", "Thành công", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = dlg.FileName,
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất PDF: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddCellToTable(PdfPTable table, string text, Font font, BaseColor bgColor = null, int align = Element.ALIGN_CENTER)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.HorizontalAlignment = align;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Padding = 6; 
            if (bgColor != null) cell.BackgroundColor = bgColor;
            table.AddCell(cell);
        }
        private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (!IsUserClickingOnRow(e)) dgHoaDon.UnselectAll(); }
        private bool IsUserClickingOnRow(MouseButtonEventArgs e) { return false; }
        private void dgHoaDon_PreviewKeyDown(object sender, KeyEventArgs e) { }
        private void dgHoaDon_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (dgHoaDon.SelectedItem is InvoiceViewModel item) { new ChiTietHoaDonWindow(item).ShowDialog(); LoadDataFromDatabase(); LoadNotifications(); } }
        private void lvPendingInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (lvPendingInvoices.SelectedItem is InvoiceViewModel item) { if (btnNoti != null) btnNoti.IsChecked = false; if (new ChiTietHoaDonWindow(item).ShowDialog() == true) { LoadDataFromDatabase(); LoadNotifications(); } } }
        private bool IsBoss() { if (UserSession.CurrentUser == null || string.IsNullOrEmpty(UserSession.CurrentUser.Chucvu)) return false; return _approverRoles.Any(r => r.Equals(UserSession.CurrentUser.Chucvu.Trim(), StringComparison.OrdinalIgnoreCase)); }
        private void LoadNotifications() { if (!IsBoss()) { gridNotification.Visibility = Visibility.Collapsed; return; } gridNotification.Visibility = Visibility.Visible; try { string sql = @"SELECT SOHDNHAP AS Ma, (SELECT TENNCC FROM NHACUNGCAP WHERE MANCC = H.MANCC) AS DoiTac, NGAYLAP, TONGTIEN, PheDuyet, TRANGTHAI, 'Nhập' AS LoaiReal FROM HOADONNHAP H WHERE PheDuyet > 0 UNION ALL SELECT SOHDXUAT AS Ma, (SELECT TENKH FROM KHACHHANG WHERE MAKH = H.MAKH) AS DoiTac, NGAYLAP, TONGTIEN, PheDuyet, TRANGTHAI, 'Xuất' AS LoaiReal FROM HOADONXUAT H WHERE PheDuyet > 0 ORDER BY NGAYLAP DESC"; var dt = Database.GetTable(sql); var list = new List<InvoiceViewModel>(); foreach (DataRow r in dt.Rows) { int pFlag = Convert.ToInt32(r["PheDuyet"]); list.Add(new InvoiceViewModel { MaHD = r["Ma"].ToString(), DoiTac = r["DoiTac"].ToString(), NgayLap = DateTime.Parse(r["NGAYLAP"].ToString()), TongTien = Convert.ToDecimal(r["TONGTIEN"]), PheDuyet = pFlag, TrangThai = "Chờ duyệt", LoaiHD = r["LoaiReal"].ToString() }); } lvPendingInvoices.ItemsSource = list; if (list.Count > 0) { bdBadge.Visibility = Visibility.Visible; txtBadgeCount.Text = list.Count.ToString(); } else { bdBadge.Visibility = Visibility.Collapsed; } } catch { } }
        private void btnAddNew_Click(object sender, RoutedEventArgs e) { if (new AddInvoiceWindow().ShowDialog() == true) { LoadDataFromDatabase(); LoadNotifications(); } }
        private void btnEdit_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.Tag is InvoiceViewModel item) { if (!IsBoss()) { MessageBox.Show("Nhân viên chỉ được tạo mới.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information); return; } var editWindow = new EditInvoiceWindow(item); if (editWindow.ShowDialog() == true) { LoadDataFromDatabase(); LoadNotifications(); } } }
        private void btnDelete_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.Tag is InvoiceViewModel item) { if (!IsBoss()) { MessageBox.Show("Nhân viên không được xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information); return; } if (MessageBox.Show("Xóa VĨNH VIỄN?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes) DeleteInvoicePermanently(item); } }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {

            var listData = dgHoaDon.ItemsSource as System.Collections.IEnumerable;

            if (listData == null)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"DanhSachHoaDon_{DateTime.Now:ddMMyyyy_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Danh sách hóa đơn");

                        worksheet.Cell(1, 1).Value = "Mã Hóa Đơn";
                        worksheet.Cell(1, 2).Value = "Đối Tác / Khách Hàng";
                        worksheet.Cell(1, 3).Value = "Ngày Lập";
                        worksheet.Cell(1, 4).Value = "Loại HĐ";
                        worksheet.Cell(1, 5).Value = "Tổng Tiền (VNĐ)";
                        worksheet.Cell(1, 6).Value = "Trạng Thái";

                        var headerRange = worksheet.Range("A1:F1");
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        int row = 2;
                        foreach (var itemObj in listData)
                        {
                            if (itemObj is InvoiceViewModel item)
                            {
                                worksheet.Cell(row, 1).Value = item.MaHD;
                                worksheet.Cell(row, 2).Value = item.DoiTac;

                                worksheet.Cell(row, 3).Value = item.NgayLap;
                                worksheet.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

                                worksheet.Cell(row, 4).Value = item.LoaiHD;
                                worksheet.Cell(row, 5).Value = item.TongTien;
                                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0"; 

                                worksheet.Cell(row, 6).Value = item.TrangThai;

                                if (item.TrangThai.Contains("thanh toán"))
                                    worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Green;
                                else if (item.TrangThai.Contains("hủy"))
                                    worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Red;
                                else
                                    worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.Orange;

                                row++;
                            }
                        }

                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(saveFileDialog.FileName);
                    }


                    if (MessageBox.Show("Xuất Excel thành công!\nBạn có muốn mở file ngay không?", "Thành công", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = saveFileDialog.FileName,
                                UseShellExecute = true
                            });
                        }
                        catch {  }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void DeleteInvoicePermanently(InvoiceViewModel item)
        {
            string cleanID = item.MaHD.Trim();
            bool isExport = item.LoaiHD == "Xuất" || cleanID.StartsWith("HDX", StringComparison.OrdinalIgnoreCase);

            string tblH = isExport ? "HOADONXUAT" : "HOADONNHAP";
            string tblD = isExport ? "CTHDXUAT" : "CTHDNHAP";
            string tblP = isExport ? "PHIEUXUAT" : "PHIEUNHAP"; 
            string colID = isExport ? "SOHDXUAT" : "SOHDNHAP";

            using (var conn = new SQLiteConnection(Database.ConnectionString))
            {
                conn.Open();
                new SQLiteCommand("PRAGMA foreign_keys = OFF;", conn).ExecuteNonQuery();

                using (var trans = conn.BeginTransaction())
                {
                    try
                    {

                        var cmdDelD = new SQLiteCommand($"DELETE FROM {tblD} WHERE {colID}=@id", conn, trans);
                        cmdDelD.Parameters.AddWithValue("@id", cleanID);
                        cmdDelD.ExecuteNonQuery();

                        var cmdDelP = new SQLiteCommand($"DELETE FROM {tblP} WHERE {colID}=@id", conn, trans);
                        cmdDelP.Parameters.AddWithValue("@id", cleanID);
                        cmdDelP.ExecuteNonQuery();

                        var cmdDelH = new SQLiteCommand($"DELETE FROM {tblH} WHERE {colID}=@id", conn, trans);
                        cmdDelH.Parameters.AddWithValue("@id", cleanID);
                        int rowsAffected = cmdDelH.ExecuteNonQuery();

                        new SQLiteCommand($"DELETE FROM YEUCAU_SUA WHERE MAHD='{cleanID}'", conn, trans).ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Không tìm thấy hóa đơn {cleanID} để xóa!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        trans.Commit();
                        MessageBox.Show("Đã xóa hóa đơn và hủy yêu cầu kho thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadDataFromDatabase();
                        LoadNotifications();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show("Lỗi khi xóa: " + ex.Message, "Lỗi SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        new SQLiteCommand("PRAGMA foreign_keys = ON;", conn).ExecuteNonQuery();
                    }
                }
            }
        }
    }
}