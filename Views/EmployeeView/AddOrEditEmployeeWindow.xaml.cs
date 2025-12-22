using Microsoft.Win32; // Dùng cho OpenFileDialog
using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Data.SQLite;
using System.Linq;

namespace PharmaDistributionApp.Views.EmployeeView
{
    public partial class AddOrEditEmployeeWindow : Window
    {
        // Biến lưu trữ nhân viên đang thao tác
        public Employee CurrentEmployee { get; set; }
        private bool _isEditMode = false; // Cờ đánh dấu chế độ Sửa

        // CONSTRUCTOR 1: Dùng cho THÊM MỚI (Không tham số)
        public AddOrEditEmployeeWindow()
        {
            InitializeComponent();

            _isEditMode = false;
            Title = "Thêm nhân viên mới";
            txtHeaderTitle.Text = "THÊM NHÂN VIÊN MỚI";

            CurrentEmployee = new Employee();
            CurrentEmployee.TrangThai = 1; // Mặc định: Đang hoạt động

            this.DataContext = CurrentEmployee;
        }

        // CONSTRUCTOR 2: Dùng cho SỬA (Có tham số Employee)
        public AddOrEditEmployeeWindow(Employee empToEdit)
        {
            InitializeComponent();

            _isEditMode = true;
            Title = "Cập nhật thông tin nhân viên";
            txtHeaderTitle.Text = "CẬP NHẬT NHÂN VIÊN";

            // QUAN TRỌNG: Clone dữ liệu ra một object mới
            // Để tránh việc chỉnh sửa trực tiếp vào danh sách gốc khi chưa bấm Lưu
            CurrentEmployee = new Employee()
            {
                Manv = empToEdit.Manv,
                Tennv = empToEdit.Tennv,
                Cccd = empToEdit.Cccd, // Đừng quên copy CCCD
                GioiTinh = empToEdit.GioiTinh,
                Chucvu = empToEdit.Chucvu,
                Email = empToEdit.Email,
                Sdt = empToEdit.Sdt,
                Diachi = empToEdit.Diachi,
                Ngaysinh = empToEdit.Ngaysinh,
                TrangThai = empToEdit.TrangThai,
                AvatarBlob = empToEdit.AvatarBlob
            };

            // Khóa ô Mã nhân viên (Không cho sửa khóa chính)
            txtManv.IsReadOnly = true;
            txtManv.Background = Brushes.WhiteSmoke;

            this.DataContext = CurrentEmployee;
        }

        // Sự kiện: Tải ảnh lên
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

                    // Refresh lại DataContext để cập nhật hình ảnh trên giao diện
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

        // Sự kiện: Nút Lưu
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra dữ liệu bắt buộc
            if (string.IsNullOrWhiteSpace(CurrentEmployee.Manv) || string.IsNullOrWhiteSpace(CurrentEmployee.Tennv))
            {
                MessageBox.Show("Vui lòng nhập Mã nhân viên và Họ tên!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Lấy giá trị Trạng thái từ ComboBox (nếu Binding không tự nhận)
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

                this.DialogResult = true; // Trả về true để màn hình danh sách biết mà reload
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sự kiện: Nút Hủy
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // --- CÁC HÀM TƯƠNG TÁC DATABASE ---

        private void InsertEmployeeToDatabase()
        {
            string sql = "INSERT INTO NHANVIEN (MANV, TENNV, CCCD, GIOITINH, CHUCVU, EMAIL, SDT, DIACHI, NGAYSINH, TRANGTHAI, AVATAR) " +
                         "VALUES (@Manv, @Tennv, @Cccd, @GioiTinh, @Chucvu, @Email, @Sdt, @Diachi, @Ngaysinh, @TrangThai, @Avatar)";

            var parameters = new Dictionary<string, object>
            {
                { "@Manv", CurrentEmployee.Manv },
                { "@Tennv", CurrentEmployee.Tennv },
                { "@Cccd", CurrentEmployee.Cccd ?? "" },
                { "@GioiTinh", CurrentEmployee.GioiTinh ?? "" },
                { "@Chucvu", CurrentEmployee.Chucvu ?? "" },
                { "@Email", CurrentEmployee.Email ?? "" },
                { "@Sdt", CurrentEmployee.Sdt ?? "" },
                { "@Diachi", CurrentEmployee.Diachi ?? "" },
                { "@Ngaysinh", CurrentEmployee.Ngaysinh ?? (object)DBNull.Value },
                { "@TrangThai", CurrentEmployee.TrangThai },
                { "@Avatar", CurrentEmployee.AvatarBlob ?? (object)DBNull.Value }
            };

            // SỬA Ở ĐÂY: Chuyển Dictionary thành mảng SQLiteParameter[]
            var sqliteParams = parameters
                .Select(p => new SQLiteParameter(p.Key, p.Value))
                .ToArray();

            Database.ExecuteNonQuery(sql, sqliteParams);
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

            var parameters = new Dictionary<string, object>
            {
                { "@Manv", CurrentEmployee.Manv },
                { "@Tennv", CurrentEmployee.Tennv },
                { "@Cccd", CurrentEmployee.Cccd ?? "" },
                { "@GioiTinh", CurrentEmployee.GioiTinh ?? "" },
                { "@Chucvu", CurrentEmployee.Chucvu ?? "" },
                { "@Email", CurrentEmployee.Email ?? "" },
                { "@Sdt", CurrentEmployee.Sdt ?? "" },
                { "@Diachi", CurrentEmployee.Diachi ?? "" },
                { "@Ngaysinh", CurrentEmployee.Ngaysinh ?? (object)DBNull.Value },
                { "@TrangThai", CurrentEmployee.TrangThai },
                { "@Avatar", CurrentEmployee.AvatarBlob ?? (object)DBNull.Value }
            };

            // SỬA Ở ĐÂY: Chuyển Dictionary thành mảng SQLiteParameter[]
            var sqliteParams = parameters
                .Select(p => new SQLiteParameter(p.Key, p.Value))
                .ToArray();

            Database.ExecuteNonQuery(sql, sqliteParams);
        }
    }
}