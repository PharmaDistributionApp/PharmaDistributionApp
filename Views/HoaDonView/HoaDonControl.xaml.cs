using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using MaterialDesignThemes.Wpf; // Cần thiết cho PackIconKind
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views
{
    public partial class HoaDonControl : UserControl
    {
        // --- CÁC BIẾN TOÀN CỤC ---
        private string _currentTab = "Xuat"; // Tab hiện tại
        private decimal _maxInvoiceValue = 100000000; // Giá trị max mặc định cho Slider
        private bool _isSyncing = false; // Cờ hiệu để tránh vòng lặp khi đồng bộ Slider <-> TextBox
        private bool _isSortAscending = false; // Trạng thái sắp xếp (False = Giảm dần/Mới nhất)

        public HoaDonControl()
        {
            InitializeComponent();

            // 1. Khởi tạo dữ liệu cho bộ lọc
            InitFilterData();

            // 2. Cập nhật giao diện Tab (Màu sắc nút bấm)
            UpdateTabVisuals();

            // 3. Tải dữ liệu từ database
            LoadDataFromDatabase(true);
        }

        // ==========================================================
        // KHỞI TẠO DỮ LIỆU BỘ LỌC
        // ==========================================================
        private void InitFilterData()
        {
            // Combo Tháng
            cbbMonth.Items.Clear();
            cbbMonth.Items.Add("Tất cả");
            for (int i = 1; i <= 12; i++) cbbMonth.Items.Add(i.ToString());
            cbbMonth.SelectedIndex = 0;

            // Combo Năm (5 năm gần nhất)
            cbbYear.Items.Clear();
            cbbYear.Items.Add("Tất cả");
            int year = DateTime.Now.Year;
            for (int i = year; i >= year - 5; i--) cbbYear.Items.Add(i.ToString());
            cbbYear.SelectedIndex = 0;

            // Combo Trạng thái
            UpdateStatusComboBox();
        }

        private void UpdateStatusComboBox()
        {
            cbbStatus.Items.Clear();
            cbbStatus.Items.Add(new ComboBoxItem { Content = "Tất cả", Tag = "All", IsSelected = true });

            // Sử dụng chung 3 trạng thái chuẩn cho cả Nhập và Xuất
            cbbStatus.Items.Add(CreateColorItem("Đã thanh toán", "#38A169")); // Xanh lá
            cbbStatus.Items.Add(CreateColorItem("Chờ thanh toán", "#DD6B20")); // Cam/Vàng
            cbbStatus.Items.Add(CreateColorItem("Đã hủy", "#E53E3E"));         // Đỏ
        }

        // Hàm hỗ trợ tạo Item có màu sắc (Chấm tròn + Chữ)
        private ComboBoxItem CreateColorItem(string text, string hexColor)
        {
            StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal };

            Ellipse dot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = (Brush)new BrushConverter().ConvertFrom(hexColor),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock tb = new TextBlock
            {
                Text = text,
                Foreground = (Brush)new BrushConverter().ConvertFrom(hexColor),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(dot);
            panel.Children.Add(tb);

            return new ComboBoxItem { Content = panel, Tag = text };
        }

        // ==========================================================
        // XỬ LÝ TẢI VÀ LỌC DỮ LIỆU (CORE LOGIC)
        // ==========================================================
        private void LoadDataFromDatabase(bool updateSliderMax = false)
        {
            // Kiểm tra an toàn: Nếu bảng chưa được khởi tạo thì thoát
            if (dgHoaDon == null) return;

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    List<InvoiceViewModel> displayList = new List<InvoiceViewModel>();

                    // --- BƯỚC 1: LẤY DỮ LIỆU THÔ TỪ DB (JOIN) ---
                    if (_currentTab == "Xuat")
                    {
                        var query = from hd in context.Hoadonxuats
                                    join kh in context.Khachhangs on hd.Makh equals kh.Makh into khGroup
                                    from k in khGroup.DefaultIfEmpty()
                                    select new { HD = hd, Ten = k != null ? k.Tenkh : "Khách lẻ" };

                        var rawList = query.ToList();

                        // Cập nhật giá trị Max cho Slider nếu cần
                        if (updateSliderMax) UpdateMaxValue(rawList.Max(x => x.HD.Tongtien));

                        // Chuyển đổi sang ViewModel
                        displayList = rawList.Select(x => Parse(x.HD.Sohdxuat, x.Ten, x.HD.Ngaylap, x.HD.Tongtien, x.HD.Trangthai, "Xuất")).ToList();
                        colDoiTac.Header = "Khách hàng";
                    }
                    else // Tab Nhập
                    {
                        var query = from hd in context.Hoadonnhaps
                                    join ncc in context.Nhacungcaps on hd.Mancc equals ncc.Mancc into nccGroup
                                    from n in nccGroup.DefaultIfEmpty()
                                    select new { HD = hd, Ten = n != null ? n.Tenncc : "NCC Vãng lai" };

                        var rawList = query.ToList();

                        if (updateSliderMax) UpdateMaxValue(rawList.Max(x => x.HD.Tongtien));

                        displayList = rawList.Select(x => Parse(x.HD.Sohdnhap, x.Ten, x.HD.Ngaylap, x.HD.Tongtien, x.HD.Trangthai, "Nhập")).ToList();
                        colDoiTac.Header = "Nhà cung cấp";
                    }

                    // --- BƯỚC 2: ÁP DỤNG CÁC BỘ LỌC ---

                    // 2.1. Tìm kiếm (Tên hoặc Mã)
                    if (txtSearch != null && !string.IsNullOrEmpty(txtSearch.Text))
                    {
                        string k = txtSearch.Text.ToLower();
                        displayList = displayList.Where(x => x.MaHD.ToLower().Contains(k) || x.DoiTac.ToLower().Contains(k)).ToList();
                    }

                    // 2.2. Thời gian (Tháng/Năm)
                    if (cbbMonth.SelectedIndex > 0)
                        displayList = displayList.Where(x => x.NgayLap.Month == int.Parse(cbbMonth.SelectedItem.ToString())).ToList();

                    if (cbbYear.SelectedIndex > 0)
                        displayList = displayList.Where(x => x.NgayLap.Year == int.Parse(cbbYear.SelectedItem.ToString())).ToList();

                    // 2.3. Trạng thái
                    if (cbbStatus.SelectedItem is ComboBoxItem selectedItem)
                    {
                        string status = selectedItem.Tag?.ToString();
                        if (status != "All" && !string.IsNullOrEmpty(status))
                            displayList = displayList.Where(x => x.TrangThai == status).ToList();
                    }

                    // 2.4. Khoảng tiền (Slider)
                    if (sldPrice != null && sldPrice.Value < (double)_maxInvoiceValue)
                    {
                        displayList = displayList.Where(x => x.TongTien <= (decimal)sldPrice.Value).ToList();
                    }

                    // --- BƯỚC 3: SẮP XẾP (SORTING) ---
                    string sortType = (cbbSortCriteria.SelectedItem as ComboBoxItem)?.Tag.ToString();

                    if (sortType == "TongTien")
                    {
                        displayList = _isSortAscending
                            ? displayList.OrderBy(x => x.TongTien).ToList()
                            : displayList.OrderByDescending(x => x.TongTien).ToList();
                    }
                    else if (sortType == "Ten")
                    {
                        displayList = _isSortAscending
                            ? displayList.OrderBy(x => x.DoiTac).ToList()
                            : displayList.OrderByDescending(x => x.DoiTac).ToList();
                    }
                    else // Mặc định: Ngày lập
                    {
                        displayList = _isSortAscending
                            ? displayList.OrderBy(x => x.NgayLap).ToList()
                            : displayList.OrderByDescending(x => x.NgayLap).ToList();
                    }

                    // Gán kết quả vào bảng
                    dgHoaDon.ItemsSource = displayList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        // Hàm cập nhật giá trị Max cho Slider và TextBox
        private void UpdateMaxValue(double? maxVal)
        {
            _maxInvoiceValue = (decimal)(maxVal ?? 100000000); // Mặc định 100tr nếu ko có dữ liệu

            if (sldPrice != null)
            {
                sldPrice.Maximum = (double)_maxInvoiceValue;
                sldPrice.Value = (double)_maxInvoiceValue;
            }

            if (txtSliderValue != null)
            {
                _isSyncing = true;
                txtSliderValue.Text = _maxInvoiceValue.ToString("N0");
                _isSyncing = false;
            }
        }

        // [QUAN TRỌNG] Hàm chuyển đổi dữ liệu và chuẩn hóa Trạng thái
        private InvoiceViewModel Parse(string ma, string ten, string ngay, double? tien, string tt, string loai)
        {
            DateTime d = DateTime.Now;
            if (!string.IsNullOrEmpty(ngay)) DateTime.TryParse(ngay, out d);

            // Logic chuẩn hóa trạng thái hiển thị
            string trangThaiHienThi = tt;

            if (loai == "Nhập")
            {
                // Mapping trạng thái cũ -> Chuẩn mới
                if (tt == "Đã nhập kho" || tt == "Hoàn tất" || tt == "Nhập kho")
                    trangThaiHienThi = "Đã thanh toán";
                else if (tt == "Chờ kiểm hàng" || tt == "Mới tạo")
                    trangThaiHienThi = "Chờ thanh toán";
                else if (tt == "Hủy" || tt == "Đã hủy" || tt == "Cancel")
                    trangThaiHienThi = "Đã hủy";
                else if (string.IsNullOrEmpty(tt))
                    trangThaiHienThi = "Chờ thanh toán";
            }
            else // Xuất
            {
                if (string.IsNullOrEmpty(tt)) trangThaiHienThi = "Chờ thanh toán";
            }

            return new InvoiceViewModel
            {
                MaHD = ma,
                DoiTac = ten,
                NgayLap = d,
                TongTien = (decimal)(tien ?? 0),
                TrangThai = trangThaiHienThi,
                LoaiHD = loai
            };
        }

        // ==========================================================
        // CÁC SỰ KIỆN GIAO DIỆN (EVENTS)
        // ==========================================================

        // 1. Chuyển Tab (Nhập / Xuất)
        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null || btn.Tag.ToString() == _currentTab) return;

            _currentTab = btn.Tag.ToString();

            UpdateTabVisuals();
            // Gọi lại hàm này để reset về "Tất cả" nhưng vẫn giữ danh sách màu chuẩn
            UpdateStatusComboBox();
            LoadDataFromDatabase(true);
        }

        private void UpdateTabVisuals()
        {
            var activeBg = (Brush)new BrushConverter().ConvertFrom("#4C70BA");
            var activeFg = Brushes.White;
            var inactiveBg = Brushes.White;
            var inactiveFg = (Brush)new BrushConverter().ConvertFrom("#4C70BA");

            if (_currentTab == "Xuat")
            {
                btnTabXuat.Background = activeBg; btnTabXuat.Foreground = activeFg;
                btnTabNhap.Background = inactiveBg; btnTabNhap.Foreground = inactiveFg;
            }
            else
            {
                btnTabNhap.Background = activeBg; btnTabNhap.Foreground = activeFg;
                btnTabXuat.Background = inactiveBg; btnTabXuat.Foreground = inactiveFg;
            }
        }

        // 2. Logic Sắp xếp
        private void cbbSortCriteria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSortIcon();
            LoadDataFromDatabase(false);
        }

        private void btnSortDirection_Click(object sender, RoutedEventArgs e)
        {
            _isSortAscending = !_isSortAscending;
            UpdateSortIcon();
            LoadDataFromDatabase(false);
        }

        private void UpdateSortIcon()
        {
            // Kiểm tra null để tránh lỗi khi khởi động
            if (iconSort == null || cbbSortCriteria == null) return;

            string criteria = (cbbSortCriteria.SelectedItem as ComboBoxItem)?.Tag.ToString();

            if (criteria == "TongTien")
            {
                iconSort.Kind = _isSortAscending ? PackIconKind.SortNumericAscending : PackIconKind.SortNumericDescending;
            }
            else if (criteria == "Ten")
            {
                iconSort.Kind = _isSortAscending ? PackIconKind.SortAlphabeticalAscending : PackIconKind.SortAlphabeticalDescending;
            }
            else
            {
                iconSort.Kind = _isSortAscending ? PackIconKind.SortCalendarAscending : PackIconKind.SortCalendarDescending;
            }
        }

        // 3. Logic Đồng bộ Slider <-> TextBox
        private void sldPrice_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSyncing || txtSliderValue == null) return;

            _isSyncing = true;
            txtSliderValue.Text = e.NewValue.ToString("N0");
            _isSyncing = false;
        }

        private void txtSliderValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing || sldPrice == null) return;

            _isSyncing = true;
            try
            {
                // Xóa dấu phẩy để lấy số thô
                string rawText = txtSliderValue.Text.Replace(",", "").Replace(".", "").Trim();

                if (double.TryParse(rawText, out double value))
                {
                    if (value > sldPrice.Maximum) sldPrice.Value = sldPrice.Maximum;
                    else sldPrice.Value = value;

                    // Format lại số hiển thị
                    txtSliderValue.Text = value.ToString("N0");
                    txtSliderValue.CaretIndex = txtSliderValue.Text.Length;
                }
                else if (string.IsNullOrEmpty(rawText))
                {
                    sldPrice.Value = 0;
                }
            }
            catch { }
            _isSyncing = false;
        }

        // 4. Các nút chức năng khác
        private void btnApplyFilter_Click(object sender, RoutedEventArgs e) => LoadDataFromDatabase(false);

        private void btnResetFilter_Click(object sender, RoutedEventArgs e)
        {
            cbbMonth.SelectedIndex = 0;
            cbbYear.SelectedIndex = 0;
            if (cbbStatus.Items.Count > 0) cbbStatus.SelectedIndex = 0;

            // Reset Slider về Max
            if (sldPrice != null) sldPrice.Value = sldPrice.Maximum;

            txtSearch.Text = "";
            LoadDataFromDatabase(false);
        }

        private void btnToggleFilter_Click(object sender, RoutedEventArgs e)
        {
            if (FilterPanel != null)
                FilterPanel.Visibility = (FilterPanel.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadDataFromDatabase(false);

        private void dgHoaDon_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgHoaDon.SelectedItem is InvoiceViewModel item)
                new ChiTietHoaDonWindow(item).ShowDialog();
        }
    }
}