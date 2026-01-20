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
using PharmaDistributionApp.Models;

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

        private void ClearGridSelection()
        {
            if (dgvSanPham != null)
            {
                dgvSanPham.UnselectAll();
                dgvSanPham.SelectedItem = null;
            }
        }
        private void LoadData()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                { 
                    var data = (from sp in context.Sanphams
                                join l in context.Loaisps on sp.Maloai equals l.Maloai into tableLoai
                                from l in tableLoai.DefaultIfEmpty()
                                join ncc in context.Nhacungcaps on sp.Nhacungcap equals ncc.Mancc into tableNCC
                                from ncc in tableNCC.DefaultIfEmpty()
                                join tk in context.Tonkhos on sp.Masp equals tk.Masp into tableKho
                                let tongTon = tableKho.Sum(x => (int?)x.Soluongton) ?? 0
                                orderby sp.Masp ascending

                                select new
                                {
                                    sp.Masp,
                                    sp.Tensp,
                                    sp.Dvt,
                                    sp.Giaban,
                                    sp.Nhacungcap,
                                    sp.Nuocsx, 

                                    TenLoai = (l != null) ? l.Tenloai : "Khác",
                                    Tenncc = (ncc != null) ? ncc.Tenncc : "Chưa xác định",
                                    SoLuongTon = tongTon
                                }).ToList();

                    dgvSanPham.ItemsSource = data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu sản phẩm: " + ex.Message);
            }
        }
        private void txtTimKiem_GotFocus(object sender, RoutedEventArgs e)
        {
            ClearGridSelection();
            if (dgvSanPham != null)
            {
                dgvSanPham.UnselectAll();
                dgvSanPham.SelectedItem = null;
            }
        }
        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearGridSelection();
            if (dgvSanPham == null) return;
            string keyword = txtTimKiem.Text.ToLower().Trim();

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var baseQuery = from sp in context.Sanphams
                                    join l in context.Loaisps on sp.Maloai equals l.Maloai into tableJoined
                                    from l in tableJoined.DefaultIfEmpty()
                                    join ncc in context.Nhacungcaps on sp.Nhacungcap equals ncc.Mancc into tableNCC
                                    from ncc in tableNCC.DefaultIfEmpty()
                                    select new
                                    {
                                        sp.Masp,
                                        sp.Tensp,
                                        sp.Dvt,
                                        sp.Giaban,
                                        Tenncc = (ncc != null) ? ncc.Tenncc : "Chưa xác định",
                                        TenLoai = (l != null) ? l.Tenloai : "Khác",
                                    };

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        var result = baseQuery.Where(x =>
                            x.Masp.ToLower().Contains(keyword) ||
                            x.Tensp.ToLower().Contains(keyword) ||
                            x.TenLoai.ToLower().Contains(keyword) ||
                           x.Tenncc.ToLower().Contains(keyword)
                        )
                        .OrderBy(x => x.Masp)
                        .ToList();

                        dgvSanPham.ItemsSource = result;
                    }
                    else
                    {
                        dgvSanPham.ItemsSource = baseQuery.OrderBy(x => x.Masp).ToList();
                        LoadData(); 
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi tìm kiếm: " + ex.Message);
            }
        }

        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            ClearGridSelection();
            var window = new ThemSanPhamWindow();
            window.OnProductAdded += () =>
            {
                LoadData();
            };

            window.ShowDialog();
        }

        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            ClearGridSelection();
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
                var menuItem = sender as MenuItem;
                if (menuItem == null) return;

                var contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu == null) return;

                var btn = contextMenu.PlacementTarget as Button;
                if (btn == null) return;

                object rowData = btn.DataContext;

                if (rowData != null)
                {
                    string maSPCanSua = "";
                    try
                    {
                        dynamic data = rowData;
                        maSPCanSua = data.Masp;
                    }
                    catch
                    {
                        MessageBox.Show("Dòng dữ liệu này không chứa thuộc tính 'Masp'. Vui lòng kiểm tra lại câu lệnh Select.");
                        return;
                    }

                    if (!string.IsNullOrEmpty(maSPCanSua))
                    {
                        var win = new PharmaDistributionApp.Views.ProductView.ThemSanPhamWindow(maSPCanSua);
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
                                LoadData(); 
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
            if (!dgvSanPham.IsMouseOver)
            {
                dgvSanPham.SelectedItem = null; 
                Keyboard.ClearFocus(); 
            }
        }
    }
}