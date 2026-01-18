using PharmaDistributionApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel; // Mới
using System.Data;
using System.Runtime.CompilerServices; // Mới
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;   // Để dùng CollectionViewSource
using ClosedXML.Excel; // Thư viện Excel
using Microsoft.Win32;
namespace PharmaDistributionApp.Views.EmployeeView
{
    public partial class EmployeeViewControl : UserControl, INotifyPropertyChanged
    {
        // Property binding danh sách
        public ObservableCollection<Employee> Employees { get; set; }

        // Property binding nhân viên đang chọn (Fix lỗi thiếu binding)
        private Employee _selectedEmployee;
        public Employee SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                _selectedEmployee = value;
                OnPropertyChanged();
            }
        }

        public EmployeeViewControl()
        {
            InitializeComponent();
            Employees = new ObservableCollection<Employee>();
            this.DataContext = this;
            LoadEmployeeData();
        }

        private void LoadEmployeeData()
        {
            try
            {
                string sql = "SELECT * FROM NHANVIEN";
                DataTable dt = Database.GetTable(sql);
                Employees.Clear();
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var nv = new Employee();
                        nv.Manv = row["MANV"].ToString();
                        nv.Tennv = row["TENNV"].ToString();
                        nv.GioiTinh = row["GIOITINH"] != DBNull.Value ? row["GIOITINH"].ToString() : "";
                        nv.Chucvu = row["CHUCVU"].ToString();
                        nv.Email = row["EMAIL"].ToString();
                        nv.Sdt = row["SDT"] != DBNull.Value ? row["SDT"].ToString() : "---";
                        nv.Diachi = row["DIACHI"] != DBNull.Value ? row["DIACHI"].ToString() : "---";
                        nv.TrangThai = row["TRANGTHAI"] != DBNull.Value ? Convert.ToInt32(row["TRANGTHAI"]) : 1;
                        nv.Cccd = row["CCCD"] != DBNull.Value ? row["CCCD"].ToString() : "";

                        if (row["NGAYSINH"] != DBNull.Value && DateTime.TryParse(row["NGAYSINH"].ToString(), out DateTime dateVal))
                        {
                            nv.Ngaysinh = dateVal;
                        }

                        if (row["AVATAR"] != DBNull.Value)
                        {
                            try { nv.AvatarBlob = (byte[])row["AVATAR"]; }
                            catch { nv.AvatarBlob = Array.Empty<byte>(); }
                        }
                        Employees.Add(nv);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 1. Chặn click vào khoảng trắng/Header
            var dependencyObj = e.OriginalSource as DependencyObject;
            if (dependencyObj == null) return;

            // Tìm xem cái được click có thuộc về DataGridRow nào không
            while (dependencyObj != null && dependencyObj != dgEmployee)
            {
                if (dependencyObj is DataGridRow) break;
                dependencyObj = VisualTreeHelper.GetParent(dependencyObj);
            }

            // Nếu không tìm thấy Row hoặc Item null thì thoát
            if (dependencyObj == null || dgEmployee.SelectedItem == null) return;

            // 2. Thực hiện mở Window
            if (dgEmployee.SelectedItem is Employee selectedEmp)
            {
                var detailWindow = new EmployeeDetailWindow(selectedEmp);
                detailWindow.ShowDialog(); // Chờ đóng cửa sổ

                // 3. QUAN TRỌNG: Xóa lựa chọn để làm mới trạng thái
                dgEmployee.SelectedItem = null;
                // Hoặc: SelectedEmployee = null; (vì đã binding 2 chiều)
            }
        }

        // Implementation INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(Employees);

            // Gán logic lọc
            view.Filter = FilterEmployee;

            // Làm mới view để áp dụng bộ lọc ngay lập tức
            view.Refresh();
        }
        private bool FilterEmployee(object item)
        {
            if (item is Employee emp)
            {
                // Lấy nội dung người dùng nhập (chuyển về chữ thường để không phân biệt hoa/thường)
                string searchText = txtSearch.Text.ToLower();

                // Nếu ô tìm kiếm trống thì hiện tất cả
                if (string.IsNullOrEmpty(searchText))
                    return true;

                // Kiểm tra: Mã NV hoặc Tên NV có chứa từ khóa không?
                // (Bạn có thể thêm emp.Email.Contains... nếu muốn tìm cả email)
                return (emp.Tennv != null && emp.Tennv.ToLower().Contains(searchText)) ||
                       (emp.Manv != null && emp.Manv.ToLower().Contains(searchText));
            }
            return false;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddOrEditEmployeeWindow();

            // Nếu người dùng bấm Lưu (DialogResult == true) thì tải lại danh sách
            if (addWindow.ShowDialog() == true)
            {
                LoadEmployeeData();
            }
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (Employees == null || Employees.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 2. Mở hộp thoại chọn nơi lưu file
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"DanhSachNhanVien_{DateTime.Now:ddMMyyyy_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 3. Tạo file Excel
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Danh Sách Nhân Viên");

                        // --- TẠO HEADER (DÒNG 1) ---
                        // Gán tiêu đề cột
                        worksheet.Cell(1, 1).Value = "Mã NV";
                        worksheet.Cell(1, 2).Value = "Họ và Tên";
                        worksheet.Cell(1, 3).Value = "CCCD";
                        worksheet.Cell(1, 4).Value = "Giới tính";
                        worksheet.Cell(1, 5).Value = "Ngày sinh";
                        worksheet.Cell(1, 6).Value = "Chức vụ";
                        worksheet.Cell(1, 7).Value = "SĐT";
                        worksheet.Cell(1, 8).Value = "Email";
                        worksheet.Cell(1, 9).Value = "Địa chỉ";
                        worksheet.Cell(1, 10).Value = "Trạng thái";

                        // Định dạng Header (Đậm, Nền xanh, Chữ trắng, Căn giữa)
                        var headerRange = worksheet.Range("A1:J1");
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4C70BA");
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // --- ĐỔ DỮ LIỆU ---
                        int row = 2;
                        foreach (var emp in Employees)
                        {
                            worksheet.Cell(row, 1).Value = emp.Manv;
                            worksheet.Cell(row, 2).Value = emp.Tennv;
                            worksheet.Cell(row, 3).Value = emp.Cccd;
                            worksheet.Cell(row, 4).Value = emp.GioiTinh;

                            // Định dạng ngày tháng
                            if (emp.Ngaysinh.HasValue)
                            {
                                worksheet.Cell(row, 5).Value = emp.Ngaysinh.Value;
                                worksheet.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy";
                            }

                            worksheet.Cell(row, 6).Value = emp.Chucvu;
                            worksheet.Cell(row, 7).Value = emp.Sdt;
                            worksheet.Cell(row, 8).Value = emp.Email;
                            worksheet.Cell(row, 9).Value = emp.Diachi;

                            // Xử lý hiển thị trạng thái (từ số sang chữ)
                            string trangThaiText = "Khác";
                            if (emp.TrangThai == 1) trangThaiText = "Đang hoạt động";
                            else if (emp.TrangThai == 2) trangThaiText = "Tạm nghỉ";
                            else if (emp.TrangThai == 0) trangThaiText = "Đã nghỉ việc";

                            worksheet.Cell(row, 10).Value = trangThaiText;

                            // Tô màu dòng trạng thái cho đẹp (Optional)
                            if (emp.TrangThai == 0)
                                worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.Red;
                            else if (emp.TrangThai == 1)
                                worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.Green;

                            row++;
                        }

                        // --- FORMAT CHUNG ---
                        // Tự động chỉnh độ rộng cột theo nội dung
                        worksheet.Columns().AdjustToContents();

                        // Kẻ khung viền cho toàn bộ bảng
                        var dataRange = worksheet.Range(1, 1, row - 1, 10);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        // Lưu file
                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    // Mở file sau khi lưu xong (Hỏi người dùng)
                    var result = MessageBox.Show("Xuất dữ liệu thành công! Bạn có muốn mở file ngay không?",
                                                 "Thành công",
                                                 MessageBoxButton.YesNo,
                                                 MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Mở file bằng phần mềm mặc định (Excel)
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

        // --- XỬ LÝ MENU NGỮ CẢNH (DẤU 3 CHẤM) ---
        private void BtnHanhDong_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            // Mở ContextMenu gắn liền với nút đó
            if (btn != null && btn.ContextMenu != null)
            {
                // Đặt vị trí menu ngay tại nút để nó hiện đúng chỗ
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        // --- XỬ LÝ NÚT SỬA TRONG MENU ---
        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            // Lấy MenuItem vừa bấm -> Lấy ContextMenu cha -> Lấy Button gốc -> Lấy DataContext (Employee)
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem.Parent as ContextMenu;
            var btn = contextMenu.PlacementTarget as Button;
            var selectedEmp = btn.DataContext as Employee;

            if (selectedEmp != null)
            {
                // Mở cửa sổ sửa (Tái sử dụng logic cũ của bạn)
                var editWindow = new AddOrEditEmployeeWindow(selectedEmp);

                if (editWindow.ShowDialog() == true)
                {
                    LoadEmployeeData(); // Tải lại danh sách sau khi sửa xong
                }
            }

        }

        // --- XỬ LÝ NÚT XÓA TRONG MENU ---
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            // Lấy thông tin nhân viên từ dòng hiện tại (tương tự như nút Sửa)
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem.Parent as ContextMenu;
            var btn = contextMenu.PlacementTarget as Button;
            var selectedEmp = btn.DataContext as Employee;

            if (selectedEmp != null)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa nhân viên {selectedEmp.Tennv}?",
                                             "Xác nhận xóa",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Logic xóa SQL cũ của bạn
                        string sql = "DELETE FROM NHANVIEN WHERE MANV = @Manv";
                        var parameters = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "@Manv", selectedEmp.Manv }
                        };

                        var sqliteParams = System.Linq.Enumerable.ToArray(
                            System.Linq.Enumerable.Select(parameters, p => new System.Data.SQLite.SQLiteParameter(p.Key, p.Value))
                        );

                        Database.ExecuteNonQuery(sql, sqliteParams);

                        // Xóa xong thì load lại
                        LoadEmployeeData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xóa: " + ex.Message);
                    }
                }
            }
        }
    }
}