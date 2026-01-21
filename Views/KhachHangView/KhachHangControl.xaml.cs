using ClosedXML.Excel;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Views.KhachHangView; 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PharmaDistributionApp.Views
{
    public partial class KhachHangControl : UserControl
    {
        public KhachHangControl()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {

                    var listKH = context.Khachhangs.ToList();

                    foreach (var kh in listKH)
                    {

                        var tongDoanhSo = context.Hoadonxuats
                            .Where(h => h.Makh == kh.Makh && h.Trangthai == "Đã thanh toán")
                            .Sum(h => (double?)h.Tongtien) ?? 0;

                        kh.Doanhso = (decimal)tongDoanhSo;

                    }

                    dgvKhachHang.ItemsSource = listKH;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu khách hàng: " + ex.Message);
            }
        }

        private void dgvKhachHang_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvKhachHang.SelectedItem is Khachhang selected)
            {
                ChiTietKhachHang detailWindow = new ChiTietKhachHang(selected);
                detailWindow.Owner = Window.GetWindow(this);
                detailWindow.ShowDialog();
            }
        }

        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = txtTimKiem.Text.ToLower();
            using (var context = new QuanlyphanphoiduocphamContext())
            {
                var filtered = context.Khachhangs
                    .Where(x => x.Tenkh.ToLower().Contains(keyword) || x.Makh.ToLower().Contains(keyword))
                    .ToList();

                foreach (var kh in filtered)
                {
                    kh.Doanhso = (decimal?)context.Hoadonxuats
                        .Where(h => h.Makh == kh.Makh && h.Trangthai == "Đã thanh toán")
                        .Sum(h => (double?)h.Tongtien) ?? 0m;
                }
                dgvKhachHang.ItemsSource = filtered;
            }
        }

        private void BtnHanhDong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null) btn.ContextMenu.IsOpen = true;
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e) => this.Focus();
        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            ThemSuaKhachHangWindow win = new ThemSuaKhachHangWindow();
            if (win.ShowDialog() == true) LoadData(); 
        }
        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            if (dgvKhachHang.SelectedItem is Khachhang selected)
            {
                ThemSuaKhachHangWindow win = new ThemSuaKhachHangWindow(selected);
                if (win.ShowDialog() == true) LoadData(); 
            }
        }
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var kh = menuItem.DataContext as Khachhang; 

            if (kh == null) return;

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                 
                    bool hasInvoice = context.Hoadonxuats.Any(hd => hd.Makh == kh.Makh);

                    if (hasInvoice)
                    {
                        MessageBox.Show(
                            $"Không thể xóa khách hàng '{kh.Tenkh}'.\n\n" +
                            "Lý do: Khách hàng này đã có lịch sử giao dịch (Hóa đơn xuất).\n" +
                            "Việc xóa sẽ làm mất dữ liệu lịch sử bán hàng.\n\n" +
                            "Gợi ý: Bạn có thể sửa thông tin thay vì xóa.",
                            "Không thể xóa",
                            MessageBoxButton.OK,
                            MessageBoxImage.Stop);
                        return; 
                    }

                    var result = MessageBox.Show(
                        $"Bạn có chắc chắn muốn xóa khách hàng: {kh.Tenkh}?\n" +
                        "Hành động này không thể hoàn tác.",
                        "Xác nhận xóa",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var dbKH = context.Khachhangs.Find(kh.Makh);
                        if (dbKH != null)
                        {
                            context.Khachhangs.Remove(dbKH);
                            context.SaveChanges();

                            MessageBox.Show("Đã xóa khách hàng thành công.", "Thông báo");
                            LoadData(); 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Root_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var hitResult = VisualTreeHelper.HitTest(dgvKhachHang, e.GetPosition(dgvKhachHang));

            if (hitResult == null || !IsClickOnRow(e.OriginalSource as DependencyObject))
            {
                dgvKhachHang.SelectedItem = null; 
                Keyboard.ClearFocus(); 
            }
        }

        private bool IsClickOnRow(DependencyObject target)
        {
            while (target != null)
            {
                if (target is DataGridRow) return true; 
                if (target is DataGrid) return false; 
                target = VisualTreeHelper.GetParent(target);
            }
            return false;
        }
        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            var listKhachHang = dgvKhachHang.ItemsSource as List<Khachhang>;

            if (listKhachHang == null || listKhachHang.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu khách hàng để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"DanhSachKhachHang_{DateTime.Now:ddMMyyyy_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Danh Sách Khách Hàng");

                        worksheet.Cell(1, 1).Value = "Mã KH";
                        worksheet.Cell(1, 2).Value = "Tên Khách Hàng";
                        worksheet.Cell(1, 3).Value = "Số Điện Thoại";
                        worksheet.Cell(1, 4).Value = "Email";
                        worksheet.Cell(1, 5).Value = "Địa Chỉ";
                        worksheet.Cell(1, 6).Value = "Loại KH";
                        worksheet.Cell(1, 7).Value = "Doanh Số (VNĐ)";

                        var headerRange = worksheet.Range("A1:G1");
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4C70BA");
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        int row = 2;
                        foreach (var kh in listKhachHang)
                        {
                            worksheet.Cell(row, 1).Value = kh.Makh;
                            worksheet.Cell(row, 2).Value = kh.Tenkh;
                            worksheet.Cell(row, 3).Value = kh.Sdt;
                            worksheet.Cell(row, 4).Value = kh.Email;
                            worksheet.Cell(row, 5).Value = kh.Diachi;
                            worksheet.Cell(row, 6).Value = kh.Loaikh;

                            worksheet.Cell(row, 7).Value = kh.Doanhso ?? 0;
                            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                            worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.DarkGreen;

                            row++;
                        }

                        worksheet.Columns().AdjustToContents();


                        var dataRange = worksheet.Range(1, 1, row - 1, 7);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    var result = MessageBox.Show("Xuất danh sách khách hàng thành công! Bạn có muốn mở file ngay không?",
                                                 "Thành công",
                                                 MessageBoxButton.YesNo,
                                                 MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var processStartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(processStartInfo);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Có lỗi khi xuất file khách hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }
    }
}