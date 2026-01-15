using MaterialDesignThemes.Wpf;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PharmaDistributionApp.Views
{
    public partial class HoaDonControl : UserControl
    {
        private string _currentTab = "Xuat";

        // [QUAN TRỌNG] Dùng double hoàn toàn
        private double _maxInvoiceValue = 100000000;

        private bool _isSyncing = false;
        private bool _isSortAscending = false;

        public HoaDonControl()
        {
            InitializeComponent();
            InitFilterData();
            UpdateTabVisuals();
            LoadDataFromDatabase(true);
        }

        // ==========================================================
        // 1. LOGIC TẢI DỮ LIỆU (Đã đồng bộ Double)
        // ==========================================================
        private void LoadDataFromDatabase(bool updateSliderMax = false)
        {
            if (dgHoaDon == null) return;

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    List<InvoiceViewModel> displayList = new List<InvoiceViewModel>();

                    if (_currentTab == "Xuat")
                    {
                        var query = from hd in context.Hoadonxuats
                                    join kh in context.Khachhangs on hd.Makh equals kh.Makh into khGroup
                                    from k in khGroup.DefaultIfEmpty()
                                    select new
                                    {
                                        Ma = hd.Sohdxuat,
                                        Ten = k != null ? k.Tenkh : "Khách lẻ",
                                        Ngay = hd.Ngaylap,
                                        // [SỬA LỖI]: Ép sang double ngay tại đây bất kể DB là decimal hay float
                                        Tien = (double)(hd.Tongtien ?? 0),
                                        TT = hd.Trangthai
                                    };

                        var rawList = query.ToList();

                        if (updateSliderMax && rawList.Count > 0)
                        {
                            var maxVal = rawList.Max(x => x.Tien);
                            UpdateMaxValue(maxVal);
                        }

                        displayList = rawList.Select(x => Parse(x.Ma, x.Ten, x.Ngay, x.Tien, x.TT, "Xuất")).ToList();
                        if (colDoiTac != null) colDoiTac.Header = "Khách hàng";
                    }
                    else // Tab Nhập
                    {
                        var query = from hd in context.Hoadonnhaps
                                    join ncc in context.Nhacungcaps on hd.Mancc equals ncc.Mancc into nccGroup
                                    from n in nccGroup.DefaultIfEmpty()
                                    select new
                                    {
                                        Ma = hd.Sohdnhap,
                                        Ten = n != null ? n.Tenncc : "NCC Vãng lai",
                                        Ngay = hd.Ngaylap,
                                        // [SỬA LỖI]: Ép sang double ngay tại đây
                                        Tien = (double)(hd.Tongtien ?? 0),
                                        TT = hd.Trangthai
                                    };

                        var rawList = query.ToList();

                        if (updateSliderMax && rawList.Count > 0)
                        {
                            var maxVal = rawList.Max(x => x.Tien);
                            UpdateMaxValue(maxVal);
                        }

                        displayList = rawList.Select(x => Parse(x.Ma, x.Ten, x.Ngay, x.Tien, x.TT, "Nhập")).ToList();
                        if (colDoiTac != null) colDoiTac.Header = "Nhà cung cấp";
                    }

                    // --- BỘ LỌC ---
                    if (!string.IsNullOrEmpty(txtSearch?.Text))
                    {
                        string k = txtSearch.Text.ToLower();
                        displayList = displayList.Where(x => x.MaHD.ToLower().Contains(k) || x.DoiTac.ToLower().Contains(k)).ToList();
                    }
                    if (cbbMonth.SelectedIndex > 0)
                        displayList = displayList.Where(x => x.NgayLap.Month == int.Parse(cbbMonth.SelectedItem.ToString())).ToList();
                    if (cbbYear.SelectedIndex > 0)
                        displayList = displayList.Where(x => x.NgayLap.Year == int.Parse(cbbYear.SelectedItem.ToString())).ToList();

                    if (cbbStatus.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag.ToString() != "All")
                    {
                        string status = selectedItem.Tag.ToString();
                        displayList = displayList.Where(x => x.TrangThai == status).ToList();
                    }

                    // [SỬA LỖI]: Ép kiểu giá trị Slider (double) sang (decimal) để so sánh được
                    if (sldPrice != null && (decimal)sldPrice.Value < (decimal)_maxInvoiceValue)
                    {
                        displayList = displayList.Where(x => x.TongTien <= (decimal)sldPrice.Value).ToList();
                    }

                    // --- SẮP XẾP ---
                    string sortType = (cbbSortCriteria.SelectedItem as ComboBoxItem)?.Tag.ToString();
                    if (sortType == "TongTien")
                        displayList = _isSortAscending ? displayList.OrderBy(x => x.TongTien).ToList() : displayList.OrderByDescending(x => x.TongTien).ToList();
                    else if (sortType == "Ten")
                        displayList = _isSortAscending ? displayList.OrderBy(x => x.DoiTac).ToList() : displayList.OrderByDescending(x => x.DoiTac).ToList();
                    else
                        displayList = _isSortAscending ? displayList.OrderBy(x => x.NgayLap).ToList() : displayList.OrderByDescending(x => x.NgayLap).ToList();

                    dgHoaDon.ItemsSource = displayList;
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        // [HÀM PARSE ĐƠN GIẢN]: Nhận double trực tiếp
        // [HÀM PARSE ĐÃ SỬA LỖI]
        private InvoiceViewModel Parse(string ma, string ten, DateTime? ngay, double tien, string tt, string loai)
        {
            string status = tt ?? "";
            if (string.IsNullOrEmpty(status)) status = "Chờ thanh toán";

            if (status == "Hoàn tất" || status == "Đã nhập kho") status = "Đã thanh toán";
            if (status == "Mới tạo") status = "Chờ thanh toán";
            if (status == "Hủy") status = "Đã hủy";

            return new InvoiceViewModel
            {
                MaHD = ma,
                DoiTac = ten,
                NgayLap = ngay ?? DateTime.Now,

                // [SỬA LỖI TẠI ĐÂY]: Thêm (decimal) vào trước biến tien
                TongTien = (decimal)tien,

                TrangThai = status,
                LoaiHD = loai
            };
        }

        private void UpdateMaxValue(double maxVal)
        {
            // [SỬA LỖI CS0266]: Biến _maxInvoiceValue giờ là double, không cần ép kiểu
            _maxInvoiceValue = maxVal == 0 ? 100000000 : maxVal;

            if (sldPrice != null)
            {
                sldPrice.Maximum = _maxInvoiceValue;
                sldPrice.Value = _maxInvoiceValue;
            }
            if (txtSliderValue != null)
            {
                _isSyncing = true;
                txtSliderValue.Text = _maxInvoiceValue.ToString("N0");
                _isSyncing = false;
            }
        }

        // ==========================================================
        // 2. SỰ KIỆN GIAO DIỆN
        // ==========================================================

        private void InitFilterData()
        {
            cbbMonth.Items.Clear(); cbbMonth.Items.Add("Tất cả"); for (int i = 1; i <= 12; i++) cbbMonth.Items.Add(i.ToString()); cbbMonth.SelectedIndex = 0;
            cbbYear.Items.Clear(); cbbYear.Items.Add("Tất cả"); int year = DateTime.Now.Year; for (int i = year; i >= year - 5; i--) cbbYear.Items.Add(i.ToString()); cbbYear.SelectedIndex = 0;
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
            Ellipse dot = new Ellipse { Width = 10, Height = 10, Fill = (Brush)new BrushConverter().ConvertFrom(hexColor), Margin = new Thickness(0, 0, 8, 0) };
            TextBlock tb = new TextBlock { Text = text, Foreground = (Brush)new BrushConverter().ConvertFrom(hexColor), FontWeight = FontWeights.SemiBold };
            panel.Children.Add(dot); panel.Children.Add(tb);
            return new ComboBoxItem { Content = panel, Tag = text };
        }

        private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridRow) && !(dep is DataGridColumnHeader))
                dep = VisualTreeHelper.GetParent(dep);
            if (dep == null) { dgHoaDon.UnselectAll(); Keyboard.ClearFocus(); }
        }

        private void Tab_Click(object sender, RoutedEventArgs e) { var btn = sender as Button; if (btn == null || btn.Tag.ToString() == _currentTab) return; _currentTab = btn.Tag.ToString(); UpdateTabVisuals(); UpdateStatusComboBox(); LoadDataFromDatabase(true); }

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
            if (_isSyncing || sldPrice == null) return;
            _isSyncing = true;
            try { string rawText = txtSliderValue.Text.Replace(",", "").Replace(".", "").Trim(); if (double.TryParse(rawText, out double value)) { if (value > sldPrice.Maximum) sldPrice.Value = sldPrice.Maximum; else sldPrice.Value = value; txtSliderValue.Text = value.ToString("N0"); txtSliderValue.CaretIndex = txtSliderValue.Text.Length; } else if (string.IsNullOrEmpty(rawText)) sldPrice.Value = 0; } catch { }
            _isSyncing = false;
        }

        private void btnApplyFilter_Click(object sender, RoutedEventArgs e) => LoadDataFromDatabase(false);
        private void btnResetFilter_Click(object sender, RoutedEventArgs e) { cbbMonth.SelectedIndex = 0; cbbYear.SelectedIndex = 0; if (cbbStatus.Items.Count > 0) cbbStatus.SelectedIndex = 0; if (sldPrice != null) sldPrice.Value = sldPrice.Maximum; txtSearch.Text = ""; LoadDataFromDatabase(false); }
        private void btnToggleFilter_Click(object sender, RoutedEventArgs e) => FilterPanel.Visibility = (FilterPanel.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadDataFromDatabase(false);

        private void dgHoaDon_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgHoaDon.SelectedItem is InvoiceViewModel item) { new ChiTietHoaDonWindow(item).ShowDialog(); LoadDataFromDatabase(false); }
        }

        private void dgHoaDon_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete) btnDelete_Click(sender, null);
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is InvoiceViewModel item) { new ChiTietHoaDonWindow(item).ShowDialog(); LoadDataFromDatabase(false); }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var selectedInv = (btn != null) ? btn.Tag as InvoiceViewModel : dgHoaDon.SelectedItem as InvoiceViewModel;

            if (selectedInv != null)
            {
                if (MessageBox.Show($"Bạn có chắc muốn xóa hóa đơn {selectedInv.MaHD}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        string table = selectedInv.LoaiHD == "Xuất" ? "HOADONXUAT" : "HOADONNHAP";
                        string colID = selectedInv.LoaiHD == "Xuất" ? "SOHDXUAT" : "SOHDNHAP";
                        string tableDetail = selectedInv.LoaiHD == "Xuất" ? "CTHDXUAT" : "CTHDNHAP";

                        var p = new SqlParameter[] { new SqlParameter("@id", selectedInv.MaHD) };
                        Database.ExecuteNonQuery($"DELETE FROM {tableDetail} WHERE {colID} = @id", p);
                        Database.ExecuteNonQuery($"DELETE FROM {table} WHERE {colID} = @id", p);

                        LoadDataFromDatabase(false);
                        MessageBox.Show("Đã xóa thành công!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi xóa: " + ex.Message);
                    }
                }
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tính năng in đang phát triển...", "Thông báo");
        }
    }
}