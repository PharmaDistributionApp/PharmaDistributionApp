using PharmaDistributionApp.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel; 
using System.Runtime.CompilerServices; 
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;   
using ClosedXML.Excel; 
using Microsoft.Win32;
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
            ApplyPermissions();
        }
        private void ClearGridSelection()
        {
            if (dgEmployee != null)
            {
                dgEmployee.SelectedItem = null;
                dgEmployee.UnselectAll();
            }
        }
        private void ApplyPermissions()
        {
            string userRole = UserSession.CurrentUser?.Chucvu ?? "Nhân viên";

            bool isAdminOrManager = userRole == "Giám đốc" || userRole == "Admin";
            bool isAccountant = userRole == "Kế toán";

            btnAddNew.Visibility = isAdminOrManager ? Visibility.Visible : Visibility.Collapsed;

            btnExportExcel.Visibility = (isAdminOrManager || isAccountant) ? Visibility.Visible : Visibility.Collapsed;


            var actionColumn = dgEmployee.Columns.FirstOrDefault(c => c.Header.ToString() == "Thao tác");
            if (actionColumn != null)
            {
                actionColumn.Visibility = isAdminOrManager ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!dgEmployee.IsMouseOver)
            {
                dgEmployee.SelectedItem = null;
                Keyboard.ClearFocus();
            }
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
            string userRole = UserSession.CurrentUser?.Chucvu ?? "Nhân viên";
            if (userRole != "Giám đốc" && userRole != "Admin" && userRole != "Quản lý kho" && userRole != "Kế toán")
            {
                MessageBox.Show($"Chức vụ '{userRole}' không có quyền xem thông tin chi tiết nhân sự.",
                                "Phân quyền", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }
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
        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            ClearGridSelection();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ClearGridSelection();
            var addWindow = new AddOrEditEmployeeWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadEmployeeData();
            }
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            ClearGridSelection();
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
            var contextMenu = menuItem.Parent as ContextMenu;
            var btn = contextMenu.PlacementTarget as Button;
            var selectedEmp = btn.DataContext as Employee;

            if (selectedEmp != null)
            {
                var editWindow = new AddOrEditEmployeeWindow(selectedEmp);

                if (editWindow.ShowDialog() == true)
                {
                    LoadEmployeeData();
                }
            }

        }
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
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
                        string sql = "DELETE FROM NHANVIEN WHERE MANV = @Manv";
                        var parameters = new System.Collections.Generic.Dictionary<string, object>
                        {
                            { "@Manv", selectedEmp.Manv }
                        };

                        var sqliteParams = System.Linq.Enumerable.ToArray(
                            System.Linq.Enumerable.Select(parameters, p => new System.Data.SQLite.SQLiteParameter(p.Key, p.Value))
                        );

                        Database.ExecuteNonQuery(sql, sqliteParams);
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