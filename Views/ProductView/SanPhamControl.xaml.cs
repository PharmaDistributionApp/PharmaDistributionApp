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
                    // 1. Lấy dữ liệu thô
                    var rawData = (from sp in context.Sanphams
                                   join l in context.Loaisps on sp.Maloai equals l.Maloai into tableLoai
                                   from l in tableLoai.DefaultIfEmpty()
                                   join tk in context.Tonkhos on sp.Masp equals tk.Masp into tableKho
                                   let tongTon = tableKho.Sum(x => (int?)x.Soluongton) ?? 0
                                   select new
                                   {
                                       sp.Masp,
                                       sp.Tensp,
                                       sp.Dvt,
                                       sp.Giaban,
                                       sp.Nhacungcap,
                                       sp.Ghichu,
                                       TenLoai = (l != null) ? l.Tenloai : "Khác",
                                       SoLuongTon = tongTon
                                   }).ToList();

                    // 2. Xử lý logic trạng thái màu sắc
                    var finalResult = rawData.Select(x =>
                    {
                        string trangThaiText;
                        string mauNen;
                        string mauChu;

                        // Logic màu sắc
                        if (!string.IsNullOrEmpty(x.Ghichu) && x.Ghichu == "Đang nhập")
                        {
                            trangThaiText = "Đang nhập";
                            mauNen = "#E3F2FD"; // Xanh dương nhạt
                            mauChu = "#1565C0"; // Xanh dương đậm
                        }
                        else
                        {
                            if (x.SoLuongTon > 10)
                            {
                                trangThaiText = "Còn hàng";
                                mauNen = "#E8F5E9"; // Xanh lá nhạt
                                mauChu = "#2E7D32"; // Xanh lá đậm
                            }
                            else if (x.SoLuongTon > 0 && x.SoLuongTon <= 10)
                            {
                                trangThaiText = "Sắp hết hàng";
                                mauNen = "#FFF3E0"; // Cam nhạt
                                mauChu = "#EF6C00"; // Cam đậm
                            }
                            else
                            {
                                trangThaiText = "Hết hàng";
                                mauNen = "#FFEBEE"; // Đỏ nhạt
                                mauChu = "#C62828"; // Đỏ đậm
                            }
                        }

                        return new
                        {
                            x.Masp,
                            x.Tensp,
                            x.Dvt,
                            x.Giaban,
                            x.Nhacungcap,
                            x.TenLoai,
                            x.SoLuongTon,
                            TenTrangThai = trangThaiText,
                            MauNenTrangThai = mauNen,
                            MauChuTrangThai = mauChu
                        };
                    })
                    // [QUAN TRỌNG] Sắp xếp theo Mã sản phẩm tăng dần (SP_001 -> SP_002)
                    .OrderBy(x => x.Masp)
                    .ToList();

                    dgvSanPham.ItemsSource = finalResult;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
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
                                        sp.Hoatchat
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
            MessageBox.Show("Chức năng sửa đang phát triển!", "Thông báo");
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
                    // Giả sử bạn có Window chi tiết
                    // var detailWindow = new ChiTietSanPhamWindow(masp);
                    // detailWindow.ShowDialog();
                    MessageBox.Show($"Xem chi tiết: {masp}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi mở chi tiết: " + ex.Message);
            }
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }
    }
}