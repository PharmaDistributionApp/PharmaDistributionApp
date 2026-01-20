using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Cần thêm dòng này cho MouseButtonEventArgs
using System.Windows.Media;
using PharmaDistributionApp.Models;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace PharmaDistributionApp.Views.NCCView
{
    /// <summary>
    /// Interaction logic for NhaCungCapControl.xaml
    /// </summary>
    public partial class NhaCungCapControl : UserControl
    {
        public NhaCungCapControl()
        {
            InitializeComponent();
            LoadData();
        }

        // ============================================================
        // 1. HÀM TẢI DỮ LIỆU TỪ DATABASE
        // ============================================================
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

        // ============================================================
        // 2. XỬ LÝ TÌM KIẾM
        // ============================================================
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

        // ============================================================
        // 3. CÁC SỰ KIỆN THAO TÁC (QUAN TRỌNG)
        // ============================================================

        // Nút Thêm Mới
        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            // Mở cửa sổ mode Thêm mới (không truyền tham số)
            var addWindow = new ThemSuaNhaCungCapWindow();

            // ShowDialog sẽ dừng code tại đây cho đến khi cửa sổ kia đóng lại
            if (addWindow.ShowDialog() == true)
            {
                // Nếu Lưu thành công thì tải lại danh sách
                LoadData();
            }
        }

        // Nút Xuất Excel
        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy dữ liệu từ DataGrid (Giả sử tên DataGrid là dgvNhaCungCap)
            var listNCC = dgvNhaCungCap.ItemsSource as List<Nhacungcap>;

            if (listNCC == null || listNCC.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu nhà cung cấp để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 2. Mở hộp thoại lưu file
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"DanhSachNhaCungCap_{DateTime.Now:ddMMyyyy_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 3. Tạo file Excel
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Nhà Cung Cấp");

                        // --- TẠO HEADER (Dòng 1) ---
                        worksheet.Cell(1, 1).Value = "Mã NCC";
                        worksheet.Cell(1, 2).Value = "Tên Nhà Cung Cấp";
                        worksheet.Cell(1, 3).Value = "Số Điện Thoại";
                        worksheet.Cell(1, 4).Value = "Email";
                        worksheet.Cell(1, 5).Value = "Địa Chỉ";

                        // Định dạng Header (Đậm, Nền xanh #4C70BA, Chữ trắng, Căn giữa)
                        var headerRange = worksheet.Range("A1:E1");
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4C70BA");
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // --- ĐỔ DỮ LIỆU ---
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

                        // --- FORMAT CHUNG ---
                        // Tự động chỉnh độ rộng cột theo nội dung
                        worksheet.Columns().AdjustToContents();

                        // Kẻ khung viền cho toàn bộ bảng dữ liệu
                        var dataRange = worksheet.Range(1, 1, row - 1, 5);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        // Lưu file
                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    // 4. Thông báo và hỏi mở file
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

        // --- MỚI: Xử lý Click đúp vào dòng để xem chi tiết ---
        private void dgvNhaCungCap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var ncc = dgvNhaCungCap.SelectedItem as Nhacungcap;
            if (ncc != null)
            {
                // Mở cửa sổ Chi tiết mới tạo
                var detailWindow = new ChiTietNhaCungCapWindow(ncc);
                detailWindow.ShowDialog();
            }
        }

        // --- MỚI: Xử lý nút 3 chấm để mở Menu ---
        private void BtnHanhDong_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            // Mở ContextMenu gắn liền với nút đó
            if (btn != null && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn; // Đặt vị trí menu ngay tại nút
                btn.ContextMenu.IsOpen = true; // Mở menu
            }
        }

        // --- MỚI: Xử lý nút Sửa (trong Menu) ---
        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var ncc = menuItem.DataContext as Nhacungcap;

            if (ncc != null)
            {
                // Mở cửa sổ mode Sửa (truyền ncc vào)
                var editWindow = new ThemSuaNhaCungCapWindow(ncc);

                if (editWindow.ShowDialog() == true)
                {
                    LoadData(); // Tải lại danh sách sau khi sửa xong
                }
            }
        }

        // --- MỚI: Xử lý nút Xóa (trong Menu) ---
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy Nhà cung cấp từ dòng được chọn
            var menuItem = sender as MenuItem;
            var ncc = menuItem.DataContext as Nhacungcap;

            if (ncc == null) return;

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // --- LOGIC MỚI: KIỂM TRA RÀNG BUỘC DỮ LIỆU ---
                    // Kiểm tra xem NCC này có bất kỳ hóa đơn nhập nào không (bất kể trạng thái)
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
                        return; // Dừng ngay lập tức
                    }

                    // 3. Nếu chưa có hóa đơn nào, hỏi xác nhận xóa
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
                            // Update các sản phẩm liên quan: Set Mancc = null (để SP không bị mất, chỉ mất liên kết NCC)
                            var products = context.Sanphams.Where(p => p.Nhacungcap == ncc.Mancc).ToList();
                            foreach (var p in products)
                            {
                                p.Nhacungcap = null;
                            }

                            // Xóa NCC
                            context.Nhacungcaps.Remove(dbNCC);
                            context.SaveChanges();

                            MessageBox.Show("Đã xóa nhà cung cấp thành công.", "Thông báo");
                            LoadData(); // Tải lại bảng hiển thị
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
            Keyboard.ClearFocus(); // Lệnh này giúp bỏ focus khỏi ô tìm kiếm
        }

    }
}