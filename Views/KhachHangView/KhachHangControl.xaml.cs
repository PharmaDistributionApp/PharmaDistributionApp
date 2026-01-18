using PharmaDistributionApp.Models;
using PharmaDistributionApp.Views.KhachHangView; // Namespace chứa cửa sổ ChiTietKhachHang
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.Win32;
using System.IO;
using ClosedXML.Excel;
using System.Collections.Generic;

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
                    // 1. Lấy danh sách khách hàng trực tiếp từ Database
                    // Đảm bảo Model đã có trường Loaikh và Doanhso
                    var listKH = context.Khachhangs.ToList();

                    foreach (var kh in listKH)
                    {
                        // 2. Tính toán doanh số dựa trên các hóa đơn đã thanh toán
                        // Cần ép kiểu decimal vì Doanhso trong Model là decimal?
                        var tongDoanhSo = context.Hoadonxuats
                            .Where(h => h.Makh == kh.Makh && h.Trangthai == "Đã thanh toán")
                            .Sum(h => (double?)h.Tongtien) ?? 0;

                        kh.Doanhso = (decimal)tongDoanhSo;

                        // LƯU Ý: Trường Loaikh sẽ tự động được Entity Framework nạp từ DB 
                        // nếu tên thuộc tính trong Model trùng khớp với tên cột trong SQL.
                    }

                    dgvKhachHang.ItemsSource = listKH;
                }
            }
            catch (Exception ex)
            {
                // Hiển thị lỗi chi tiết nếu định dạng ngày sinh hoặc dữ liệu sai
                MessageBox.Show("Lỗi tải dữ liệu khách hàng: " + ex.Message);
            }
        }

        // Sự kiện nháy đúp: Mở màn hình chi tiết
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
            if (win.ShowDialog() == true) LoadData(); // Load lại bảng sau khi thêm
        }
        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            if (dgvKhachHang.SelectedItem is Khachhang selected)
            {
                ThemSuaKhachHangWindow win = new ThemSuaKhachHangWindow(selected);
                if (win.ShowDialog() == true) LoadData(); // Load lại bảng sau khi sửa
            }
        }
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (dgvKhachHang.SelectedItem is Khachhang selected)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa khách hàng: {selected.Tenkh}?",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new QuanlyphanphoiduocphamContext())
                        {
                            // Kiểm tra ràng buộc hóa đơn
                            bool hasInvoice = context.Hoadonxuats.Any(h => h.Makh == selected.Makh);
                            if (hasInvoice)
                            {
                                MessageBox.Show("Không thể xóa khách hàng này vì đã có dữ liệu hóa đơn liên quan!", "Cảnh báo");
                                return;
                            }

                            var kh = context.Khachhangs.Find(selected.Makh);
                            if (kh != null)
                            {
                                context.Khachhangs.Remove(kh);
                                context.SaveChanges();
                                LoadData(); // Tải lại danh sách
                            }
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Lỗi xóa: " + ex.Message); }
                }
            }
        }
        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy dữ liệu từ DataGrid khách hàng
            var listKhachHang = dgvKhachHang.ItemsSource as List<Khachhang>;

            if (listKhachHang == null || listKhachHang.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu khách hàng để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 2. Mở hộp thoại chọn nơi lưu file
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"DanhSachKhachHang_{DateTime.Now:ddMMyyyy_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 3. Tạo file Excel bằng ClosedXML
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Danh Sách Khách Hàng");

                        // --- TẠO HEADER (Khớp với Model Khách hàng) ---
                        worksheet.Cell(1, 1).Value = "Mã KH";
                        worksheet.Cell(1, 2).Value = "Tên Khách Hàng";
                        worksheet.Cell(1, 3).Value = "Số Điện Thoại";
                        worksheet.Cell(1, 4).Value = "Email";
                        worksheet.Cell(1, 5).Value = "Địa Chỉ";
                        worksheet.Cell(1, 6).Value = "Loại KH";
                        worksheet.Cell(1, 7).Value = "Doanh Số (VNĐ)";

                        // Định dạng Header (Nền xanh #4C70BA giống giao diện chính)
                        var headerRange = worksheet.Range("A1:G1");
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4C70BA");
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // --- ĐỔ DỮ LIỆU ---
                        int row = 2;
                        foreach (var kh in listKhachHang)
                        {
                            worksheet.Cell(row, 1).Value = kh.Makh;
                            worksheet.Cell(row, 2).Value = kh.Tenkh;
                            worksheet.Cell(row, 3).Value = kh.Sdt;
                            worksheet.Cell(row, 4).Value = kh.Email; // Xuất Email ẩn
                            worksheet.Cell(row, 5).Value = kh.Diachi;
                            worksheet.Cell(row, 6).Value = kh.Loaikh;

                            // Định dạng số cho Doanh số
                            worksheet.Cell(row, 7).Value = kh.Doanhso ?? 0;
                            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                            worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.DarkGreen;

                            row++;
                        }

                        // --- FORMAT CHUNG ---
                        // Tự động chỉnh độ rộng cột theo nội dung
                        worksheet.Columns().AdjustToContents();

                        // Kẻ khung viền cho toàn bộ bảng
                        var dataRange = worksheet.Range(1, 1, row - 1, 7);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        // Lưu file
                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    // Mở file sau khi lưu xong
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