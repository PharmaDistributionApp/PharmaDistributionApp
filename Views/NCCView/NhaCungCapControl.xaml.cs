using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; 
using System.Windows.Media;
using PharmaDistributionApp.Models;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace PharmaDistributionApp.Views.NCCView
{
    public partial class NhaCungCapControl : UserControl
    {
        public NhaCungCapControl()
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
                    var listNCC = context.Nhacungcaps.OrderBy(x => x.Tenncc).ToList();
                    dgvNhaCungCap.ItemsSource = listNCC;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtTimKiem.Text == "Tìm kiếm..." || dgvNhaCungCap == null) return;

            string keyword = txtTimKiem.Text.ToLower().Trim();

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var query = context.Nhacungcaps.AsQueryable();

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        query = query.Where(x =>
                            x.Mancc.ToLower().Contains(keyword) ||
                            x.Tenncc.ToLower().Contains(keyword));
                    }
                    dgvNhaCungCap.ItemsSource = query.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi tìm kiếm: " + ex.Message);
            }
        }

        private void txtTimKiem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtTimKiem.Text == "Tìm kiếm...")
            {
                txtTimKiem.Text = "";
                txtTimKiem.Foreground = Brushes.Black;
            }
        }

        private void txtTimKiem_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTimKiem.Text))
            {
                txtTimKiem.Text = "Tìm kiếm...";
                txtTimKiem.Foreground = Brushes.Gray;
            }
        }

        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new ThemSuaNhaCungCapWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadData();
            }
        }
        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            var listNCC = dgvNhaCungCap.ItemsSource as List<Nhacungcap>;

            if (listNCC == null || listNCC.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu nhà cung cấp để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"DanhSachNhaCungCap_{DateTime.Now:ddMMyyyy_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Nhà Cung Cấp");
                        worksheet.Cell(1, 1).Value = "Mã NCC";
                        worksheet.Cell(1, 2).Value = "Tên Nhà Cung Cấp";
                        worksheet.Cell(1, 3).Value = "Số Điện Thoại";
                        worksheet.Cell(1, 4).Value = "Email";
                        worksheet.Cell(1, 5).Value = "Địa Chỉ";

                        var headerRange = worksheet.Range("A1:E1");
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4C70BA");
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        int row = 2;
                        foreach (var ncc in listNCC)
                        {
                            worksheet.Cell(row, 1).Value = ncc.Mancc;
                            worksheet.Cell(row, 2).Value = ncc.Tenncc;
                            worksheet.Cell(row, 3).Value = ncc.Sdt;
                            worksheet.Cell(row, 4).Value = ncc.Email;
                            worksheet.Cell(row, 5).Value = ncc.Diachi;

                            row++;
                        }

                        worksheet.Columns().AdjustToContents();

                        var dataRange = worksheet.Range(1, 1, row - 1, 5);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        workbook.SaveAs(saveFileDialog.FileName);
                    }


                    var result = MessageBox.Show("Xuất danh sách nhà cung cấp thành công! Bạn có muốn mở file ngay không?",
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
                    MessageBox.Show($"Có lỗi khi xuất file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void dgvNhaCungCap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var ncc = dgvNhaCungCap.SelectedItem as Nhacungcap;
            if (ncc != null)
            {
                var detailWindow = new ChiTietNhaCungCapWindow(ncc);
                detailWindow.ShowDialog();
            }
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
        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var ncc = menuItem.DataContext as Nhacungcap;

            if (ncc != null)
            {

                var editWindow = new ThemSuaNhaCungCapWindow(ncc);

                if (editWindow.ShowDialog() == true)
                {
                    LoadData(); 
                }
            }
        }

        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var ncc = menuItem.DataContext as Nhacungcap;

            if (ncc == null) return;

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    bool hasHistory = context.Hoadonnhaps.Any(hd => hd.Mancc == ncc.Mancc);

                    if (hasHistory)
                    {
                        MessageBox.Show(
                            $"Không thể xóa nhà cung cấp '{ncc.Tenncc}'.\n\n" +
                            "Lý do: Nhà cung cấp này đã có lịch sử giao dịch (Hóa đơn nhập).\n" +
                            "Việc xóa sẽ làm mất tính toàn vẹn của dữ liệu kế toán.\n\n" +
                            "Gợi ý: Bạn có thể sửa thông tin hoặc ngừng nhập hàng từ NCC này thay vì xóa.",
                            "Không thể xóa",
                            MessageBoxButton.OK,
                            MessageBoxImage.Stop);
                        return; 
                    }
                    var result = MessageBox.Show(
                        $"Bạn có chắc chắn muốn xóa nhà cung cấp: {ncc.Tenncc}?\n" +
                        "Lưu ý: Các sản phẩm thuộc NCC này sẽ bị gỡ bỏ thông tin nhà cung cấp.",
                        "Xác nhận xóa",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var dbNCC = context.Nhacungcaps.Find(ncc.Mancc);
                        if (dbNCC != null)
                        {
                            var products = context.Sanphams.Where(p => p.Nhacungcap == ncc.Mancc).ToList();
                            foreach (var p in products)
                            {
                                p.Nhacungcap = null;
                            }

                            context.Nhacungcaps.Remove(dbNCC);
                            context.SaveChanges();

                            MessageBox.Show("Đã xóa nhà cung cấp thành công.", "Thông báo");
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
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var hitResult = VisualTreeHelper.HitTest(dgvNhaCungCap, e.GetPosition(dgvNhaCungCap));

            if (hitResult == null || !IsClickOnRow(e.OriginalSource as DependencyObject))
            {
                dgvNhaCungCap.SelectedItem = null;
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

    }
}