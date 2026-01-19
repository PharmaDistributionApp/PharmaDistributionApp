using System;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Models; // Đảm bảo namespace này đúng với dự án của bạn

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class SanPhamControl : UserControl
    {
        public SanPhamControl()
        {
            InitializeComponent();
            this.Loaded += SanPhamControl_Loaded;
        }

        private void SanPhamControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        // --- 1. TẢI DỮ LIỆU (SẮP XẾP THEO MÃ SP) ---
        private void LoadData()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // Truy vấn trực tiếp và sắp xếp luôn từ Database
                    var data = (from sp in context.Sanphams
                                    // Join bảng Loại thuốc để lấy Tên loại
                                join l in context.Loaisps on sp.Maloai equals l.Maloai into tableLoai
                                from l in tableLoai.DefaultIfEmpty()

                                    // Join bảng Tồn kho để tính tổng tồn (Nếu bạn muốn xem nhanh số lượng)
                                    // Nếu muốn bỏ hẳn cột số lượng, bạn có thể xóa 2 dòng dưới này
                                join tk in context.Tonkhos on sp.Masp equals tk.Masp into tableKho
                                let tongTon = tableKho.Sum(x => (int?)x.Soluongton) ?? 0

                                // Sắp xếp theo Mã sản phẩm
                                orderby sp.Masp ascending

                                select new
                                {
                                    sp.Masp,
                                    sp.Tensp,
                                    sp.Dvt,
                                    sp.Giaban,
                                    sp.Nhacungcap,
                                    sp.Nuocsx,   // Thêm nước sản xuất (nếu cần hiển thị)

                                    TenLoai = (l != null) ? l.Tenloai : "Khác",

                                    // Tổng tồn kho (Có thể xóa nếu không cần hiện ở bảng này)
                                    SoLuongTon = tongTon
                                }).ToList();

                    // Gán dữ liệu vào lưới
                    dgvSanPham.ItemsSource = data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu sản phẩm: " + ex.Message);
            }
        }

        // --- 2. TÌM KIẾM ---
        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dgvSanPham == null) return;

            // Lấy từ khóa, nếu ô tìm kiếm đang hiện Hint (null hoặc rỗng) thì coi như keyword rỗng
            string keyword = txtTimKiem.Text.ToLower().Trim();

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var baseQuery = from sp in context.Sanphams
                                    join l in context.Loaisps on sp.Maloai equals l.Maloai into tableJoined
                                    from l in tableJoined.DefaultIfEmpty()
                                    select new
                                    {
                                        sp.Masp,
                                        sp.Tensp,
                                        sp.Dvt,
                                        sp.Giaban,
                                        sp.Nhacungcap,
                                        TenLoai = (l != null) ? l.Tenloai : "Khác",
                                    };

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        var result = baseQuery.Where(x =>
                            x.Masp.ToLower().Contains(keyword) ||
                            x.Tensp.ToLower().Contains(keyword) ||
                            x.TenLoai.ToLower().Contains(keyword) ||
                            (x.Nhacungcap != null && x.Nhacungcap.ToLower().Contains(keyword))
                        )
                        // Kết quả tìm kiếm cũng nên sắp xếp theo Mã
                        .OrderBy(x => x.Masp)
                        .ToList();

                        dgvSanPham.ItemsSource = result;
                    }
                    else
                    {
                        // Nếu xóa hết chữ tìm kiếm -> Load lại danh sách gốc theo thứ tự Mã
                        dgvSanPham.ItemsSource = baseQuery.OrderBy(x => x.Masp).ToList();
                        LoadData(); // Gọi lại LoadData để lấy đầy đủ màu sắc trạng thái
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi tìm kiếm: " + ex.Message);
            }
        }

        // --- CÁC NÚT CHỨC NĂNG ---

        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            var window = new ThemSanPhamWindow();

            // Khi thêm xong, reload lại bảng
            window.OnProductAdded += () =>
            {
                LoadData();
            };

            window.ShowDialog();
        }

        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            // Cấu hình License cho EPPlus 8
            ExcelPackage.License.SetNonCommercialPersonal("PharmaApp Student");

            if (dgvSanPham.ItemsSource == null)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var listData = dgvSanPham.ItemsSource as System.Collections.IEnumerable;
            if (listData == null) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
            saveFileDialog.FileName = "DanhSachSanPham_" + DateTime.Now.ToString("ddMMyyyy_HHmm");

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Sản Phẩm");

                        // Header
                        string[] headers = { "Mã SP", "Tên Sản Phẩm", "Đơn vị", "Loại", "Giá Bán", "Trạng Thái", "Tồn Kho", "Nhà Cung Cấp" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            worksheet.Cells[1, i + 1].Value = headers[i];
                            var cell = worksheet.Cells[1, i + 1];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(76, 112, 186));
                            cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        // Data
                        int row = 2;
                        foreach (dynamic item in listData)
                        {
                            worksheet.Cells[row, 1].Value = item.Masp;
                            worksheet.Cells[row, 2].Value = item.Tensp;
                            worksheet.Cells[row, 3].Value = item.Dvt;
                            worksheet.Cells[row, 4].Value = item.TenLoai;
                            worksheet.Cells[row, 5].Value = item.Giaban;
                            worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0 \"đ\"";
                            worksheet.Cells[row, 6].Value = item.TenTrangThai;
                            worksheet.Cells[row, 7].Value = item.SoLuongTon;
                            worksheet.Cells[row, 8].Value = item.Nhacungcap;

                            worksheet.Cells[row, 1, row, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                            row++;
                        }

                        worksheet.Cells.AutoFitColumns();
                        File.WriteAllBytes(saveFileDialog.FileName, package.GetAsByteArray());

                        var result = MessageBox.Show("Xuất Excel thành công! Bạn có muốn mở file ngay không?",
                                                     "Thành công", MessageBoxButton.YesNo, MessageBoxImage.Information);

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
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Có lỗi khi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            try
            {
                // 1. Lấy các thành phần UI từ Menu
                var menuItem = sender as MenuItem;
                if (menuItem == null) return;

                var contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu == null) return;

                var btn = contextMenu.PlacementTarget as Button;
                if (btn == null) return;

                // 2. Lấy dữ liệu dòng hiện tại (Dùng object thay vì ép kiểu cứng)
                object rowData = btn.DataContext;

                if (rowData != null)
                {
                    string maSPCanSua = "";

                    // 3. Sử dụng 'dynamic' để lấy thuộc tính Masp
                    // Cách này hoạt động với cả Class Sanpham, DTO, hoặc Anonymous Type (select new { ... })
                    try
                    {
                        dynamic data = rowData;
                        maSPCanSua = data.Masp; // Đảm bảo trong câu lệnh Select của bạn có thuộc tính tên là "Masp"
                    }
                    catch
                    {
                        MessageBox.Show("Dòng dữ liệu này không chứa thuộc tính 'Masp'. Vui lòng kiểm tra lại câu lệnh Select.");
                        return;
                    }

                    // 4. Mở cửa sổ sửa
                    if (!string.IsNullOrEmpty(maSPCanSua))
                    {
                        // Đảm bảo đã using namespace chứa ThemSanPhamWindow
                        var win = new PharmaDistributionApp.Views.ProductView.ThemSanPhamWindow(maSPCanSua);

                        // Đăng ký sự kiện: Khi sửa xong thì load lại lưới
                        win.OnProductAdded += () => LoadData();

                        win.ShowDialog();
                    }
                }
                else
                {
                    MessageBox.Show("Không lấy được dữ liệu dòng này (DataContext is null).");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            var item = dgvSanPham.SelectedItem;
            if (item != null)
            {
                try
                {
                    // Dùng Reflection lấy dữ liệu từ Anonymous Type
                    string masp = (string)item.GetType().GetProperty("Masp").GetValue(item, null);
                    string tensp = (string)item.GetType().GetProperty("Tensp").GetValue(item, null);

                    var result = MessageBox.Show($"Bạn có chắc muốn xóa sản phẩm '{tensp}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        using (var context = new QuanlyphanphoiduocphamContext())
                        {
                            var dbSp = context.Sanphams.Find(masp);
                            if (dbSp != null)
                            {   
                                context.Sanphams.Remove(dbSp);
                                context.SaveChanges();
                                LoadData(); // Tải lại để cập nhật danh sách
                                MessageBox.Show("Đã xóa thành công!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xóa: " + ex.Message);
                }
            }
        }

        private void dgvSanPham_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvSanPham.SelectedItem == null) return;

            try
            {
                dynamic item = dgvSanPham.SelectedItem;
                string masp = item.Masp;

                if (!string.IsNullOrEmpty(masp))
                {
                    var detailWindow = new ChiTietSanPhamWindow(masp);
                    detailWindow.ShowDialog();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi mở chi tiết: " + ex.Message);
            }
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Kiểm tra: Nếu con chuột KHÔNG nằm trên DataGrid thì mới bỏ chọn
            // (Nếu chuột đang ở trên DataGrid nghĩa là người dùng đang muốn chọn dòng, ta không được can thiệp)
            if (!dgvSanPham.IsMouseOver)
            {
                dgvSanPham.SelectedItem = null; // Bỏ chọn dòng hiện tại

                // Tùy chọn: Làm mất focus của ô tìm kiếm nếu muốn
                // Keyboard.ClearFocus(); 
            }
        }
    }
}