using PharmaDistributionApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Linq; // Thêm Linq để xử lý tham số
using System.Collections.Generic; // Thêm Dictionary
// [SỬA ĐỔI 1]: Thêm thư viện SQL Server
using Microsoft.Data.SqlClient;

namespace PharmaDistributionApp.Views.EmployeeView
{
    public partial class EmployeeViewControl : UserControl, INotifyPropertyChanged
    {
        public ObservableCollection<Employee> Employees { get; set; }

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
                // Hàm GetTable này đã được cập nhật sang SQL Server ở các bước trước
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
            var dependencyObj = e.OriginalSource as DependencyObject;
            if (dependencyObj == null) return;

            while (dependencyObj != null && dependencyObj != dgEmployee)
            {
                if (dependencyObj is DataGridRow) break;
                dependencyObj = VisualTreeHelper.GetParent(dependencyObj);
            }

            if (dependencyObj == null || dgEmployee.SelectedItem == null) return;

            if (dgEmployee.SelectedItem is Employee selectedEmp)
            {
                var detailWindow = new EmployeeDetailWindow(selectedEmp);
                detailWindow.ShowDialog();
                dgEmployee.SelectedItem = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(Employees);
            view.Filter = FilterEmployee;
            view.Refresh();
        }

        private bool FilterEmployee(object item)
        {
            if (item is Employee emp)
            {
                string searchText = txtSearch.Text.ToLower();
                if (string.IsNullOrEmpty(searchText))
                    return true;

                return (emp.Tennv != null && emp.Tennv.ToLower().Contains(searchText)) ||
                       (emp.Manv != null && emp.Manv.ToLower().Contains(searchText));
            }
            return false;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddOrEditEmployeeWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadEmployeeData();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmployee.SelectedItem is Employee selectedEmp)
            {
                var editWindow = new AddOrEditEmployeeWindow(selectedEmp);
                if (editWindow.ShowDialog() == true)
                {
                    LoadEmployeeData();
                }
            }
        }

        // [SỬA ĐỔI 2]: Sửa logic xóa dùng SqlParameter
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgEmployee.SelectedItem is Employee selectedEmp)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa nhân viên {selectedEmp.Tennv}?",
                                             "Xác nhận xóa",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string sql = "DELETE FROM NHANVIEN WHERE MANV = @Manv";

                        var parameters = new Dictionary<string, object>
                        {
                            { "@Manv", selectedEmp.Manv }
                        };

                        // SỬA: Dùng SqlParameter thay vì SQLiteParameter
                        var sqlParams = parameters
                            .Select(p => new SqlParameter(p.Key, p.Value))
                            .ToArray();

                        // Gọi hàm ExecuteNonQuery của SQL Server
                        Database.ExecuteNonQuery(sql, sqlParams);

                        LoadEmployeeData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xóa: " + ex.Message);
                    }
                }
            }
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (Employees == null || Employees.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"DanhSachNhanVien_{DateTime.Now:ddMMyyyy_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Danh Sách Nhân Viên");

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

                        var headerRange = worksheet.Range("A1:J1");
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4C70BA");
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        int row = 2;
                        foreach (var emp in Employees)
                        {
                            worksheet.Cell(row, 1).Value = emp.Manv;
                            worksheet.Cell(row, 2).Value = emp.Tennv;
                            worksheet.Cell(row, 3).Value = emp.Cccd;
                            worksheet.Cell(row, 4).Value = emp.GioiTinh;

                            if (emp.Ngaysinh.HasValue)
                            {
                                worksheet.Cell(row, 5).Value = emp.Ngaysinh.Value;
                                worksheet.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy";
                            }

                            worksheet.Cell(row, 6).Value = emp.Chucvu;
                            worksheet.Cell(row, 7).Value = emp.Sdt;
                            worksheet.Cell(row, 8).Value = emp.Email;
                            worksheet.Cell(row, 9).Value = emp.Diachi;

                            string trangThaiText = "Khác";
                            if (emp.TrangThai == 1) trangThaiText = "Đang hoạt động";
                            else if (emp.TrangThai == 2) trangThaiText = "Tạm nghỉ";
                            else if (emp.TrangThai == 0) trangThaiText = "Đã nghỉ việc";

                            worksheet.Cell(row, 10).Value = trangThaiText;

                            if (emp.TrangThai == 0)
                                worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.Red;
                            else if (emp.TrangThai == 1)
                                worksheet.Cell(row, 10).Style.Font.FontColor = XLColor.Green;

                            row++;
                        }

                        worksheet.Columns().AdjustToContents();

                        var dataRange = worksheet.Range(1, 1, row - 1, 10);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    var result = MessageBox.Show("Xuất dữ liệu thành công! Bạn có muốn mở file ngay không?",
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
    }
}