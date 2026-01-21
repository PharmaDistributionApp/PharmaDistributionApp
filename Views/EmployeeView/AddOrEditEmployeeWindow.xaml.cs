using Microsoft.Win32; // Dùng cho OpenFileDialog
using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Data.SQLite;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace PharmaDistributionApp.Views.EmployeeView
{
    public partial class AddOrEditEmployeeWindow : Window
    {
        public Employee CurrentEmployee { get; set; }
        private bool _isEditMode = false; 

        public AddOrEditEmployeeWindow()
        {
            InitializeComponent();

            _isEditMode = false;
            Title = "Thêm nhân viên mới";
            txtHeaderTitle.Text = "THÊM NHÂN VIÊN MỚI";

            CurrentEmployee = new Employee();
            CurrentEmployee.TrangThai = 1; 

            this.DataContext = CurrentEmployee;
        }

        public AddOrEditEmployeeWindow(Employee empToEdit)
        {
            InitializeComponent();

            _isEditMode = true;
            Title = "Cập nhật thông tin nhân viên";
            txtHeaderTitle.Text = "CẬP NHẬT NHÂN VIÊN";


            CurrentEmployee = new Employee()
            {
                Manv = empToEdit.Manv,
                Tennv = empToEdit.Tennv,
                Cccd = empToEdit.Cccd, 
                GioiTinh = empToEdit.GioiTinh,
                Chucvu = empToEdit.Chucvu,
                Email = empToEdit.Email,
                Sdt = empToEdit.Sdt,
                Diachi = empToEdit.Diachi,
                Ngaysinh = empToEdit.Ngaysinh,
                TrangThai = empToEdit.TrangThai,
                AvatarBlob = empToEdit.AvatarBlob
            };

            txtManv.IsReadOnly = true;
            txtManv.Background = Brushes.WhiteSmoke;

            this.DataContext = CurrentEmployee;
        }


        private void btnUploadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    CurrentEmployee.AvatarBlob = imageBytes;

                    var temp = CurrentEmployee;
                    this.DataContext = null;
                    this.DataContext = temp;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi đọc ảnh: " + ex.Message);
                }
            }
        }


        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CurrentEmployee.Manv) || string.IsNullOrWhiteSpace(CurrentEmployee.Tennv))
            {
                MessageBox.Show("Vui lòng nhập Mã nhân viên và Họ tên!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cboTrangThai.SelectedValue != null)
            {
                if (int.TryParse(cboTrangThai.SelectedValue.ToString(), out int status))
                {
                    CurrentEmployee.TrangThai = status;
                }
            }

            try
            {
                if (_isEditMode)
                {
                    UpdateEmployeeInDatabase();
                    MessageBox.Show("Cập nhật thành công!");
                }
                else
                {
                    InsertEmployeeToDatabase();
                    MessageBox.Show("Thêm mới thành công!");
                }

                this.DialogResult = true; 
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        
        private void InsertEmployeeToDatabase()
        {
            string sql = "INSERT INTO NHANVIEN (MANV, TENNV, CCCD, GIOITINH, CHUCVU, EMAIL, SDT, DIACHI, NGAYSINH, TRANGTHAI, AVATAR) " +
                         "VALUES (@Manv, @Tennv, @Cccd, @GioiTinh, @Chucvu, @Email, @Sdt, @Diachi, @Ngaysinh, @TrangThai, @Avatar)";

            var parameters = new SqliteParameter[]
            {
                new SqliteParameter("@Manv", CurrentEmployee.Manv),
                new SqliteParameter("@Tennv", CurrentEmployee.Tennv),
                new SqliteParameter("@Cccd", CurrentEmployee.Cccd ?? ""),
                new SqliteParameter("@GioiTinh", CurrentEmployee.GioiTinh ?? ""),
                new SqliteParameter("@Chucvu", CurrentEmployee.Chucvu ?? ""),
                new SqliteParameter("@Email", CurrentEmployee.Email ?? ""),
                new SqliteParameter("@Sdt", CurrentEmployee.Sdt ?? ""),
                new SqliteParameter("@Diachi", CurrentEmployee.Diachi ?? ""),
                new SqliteParameter("@Ngaysinh", CurrentEmployee.Ngaysinh ?? (object)DBNull.Value),
                new SqliteParameter("@TrangThai", CurrentEmployee.TrangThai),
                new SqliteParameter("@Avatar", CurrentEmployee.AvatarBlob ?? (object)DBNull.Value)
            };

            Database.ExecuteNonQuery(sql, parameters);
        }

        private void UpdateEmployeeInDatabase()
        {
            string sql = "UPDATE NHANVIEN SET " +
                         "TENNV = @Tennv, " +
                         "CCCD = @Cccd, " +
                         "GIOITINH = @GioiTinh, " +
                         "CHUCVU = @Chucvu, " +
                         "EMAIL = @Email, " +
                         "SDT = @Sdt, " +
                         "DIACHI = @Diachi, " +
                         "NGAYSINH = @Ngaysinh, " +
                         "TRANGTHAI = @TrangThai, " +
                         "AVATAR = @Avatar " +
                         "WHERE MANV = @Manv";

            var parameters = new SqliteParameter[]
            {
                new SqliteParameter("@Manv", CurrentEmployee.Manv),
                new SqliteParameter("@Tennv", CurrentEmployee.Tennv),
                new SqliteParameter("@Cccd", CurrentEmployee.Cccd ?? ""),
                new SqliteParameter("@GioiTinh", CurrentEmployee.GioiTinh ?? ""),
                new SqliteParameter("@Chucvu", CurrentEmployee.Chucvu ?? ""),
                new SqliteParameter("@Email", CurrentEmployee.Email ?? ""),
                new SqliteParameter("@Sdt", CurrentEmployee.Sdt ?? ""),
                new SqliteParameter("@Diachi", CurrentEmployee.Diachi ?? ""),
                new SqliteParameter("@Ngaysinh", CurrentEmployee.Ngaysinh ?? (object)DBNull.Value),
                new SqliteParameter("@TrangThai", CurrentEmployee.TrangThai),
                new SqliteParameter("@Avatar", CurrentEmployee.AvatarBlob ?? (object)DBNull.Value)
            };

            Database.ExecuteNonQuery(sql, parameters);
        }
    }
}