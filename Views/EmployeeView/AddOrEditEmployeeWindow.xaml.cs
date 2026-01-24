using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace PharmaDistributionApp.Views.EmployeeView
{
    public partial class AddOrEditEmployeeWindow : Window
    {
        public Employee CurrentEmployee { get; set; }
        private bool _isEditMode = false;
        private Brush _errorBrush = Brushes.Red;
        private Brush _normalBrush = (Brush)new BrushConverter().ConvertFrom("#89000000");
        private string GenerateNewManv()
        {
            List<int> existingIds = new List<int>();

            try
            {
                string sql = "SELECT MANV FROM NHANVIEN";
                var dt = Database.GetTable(sql, null);

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    string sManv = row["MANV"].ToString();
                    if (sManv.StartsWith("NV") && sManv.Length > 2)
                    {
                        string numberPart = sManv.Substring(2);
                        if (int.TryParse(numberPart, out int id))
                        {
                            existingIds.Add(id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi sinh mã nhân viên: " + ex.Message);
                return "";
            }
            existingIds.Sort();

            int nextId = 1;
            foreach (int id in existingIds)
            {
                if (id == nextId)
                {
                    nextId++;
                }
                else if (id > nextId)
                {
                    break;
                }
            }

            return $"NV{nextId:D3}";
        }

        public AddOrEditEmployeeWindow()
        {
            InitializeComponent();

            _isEditMode = false;
            Title = "Thêm nhân viên mới";
            txtHeaderTitle.Text = "THÊM NHÂN VIÊN MỚI";

            CurrentEmployee = new Employee();
            CurrentEmployee.Manv = GenerateNewManv();
            CurrentEmployee.TrangThai = 1;
            txtManv.IsReadOnly = true;
            txtManv.Background = Brushes.WhiteSmoke;

            this.DataContext = CurrentEmployee;
            RegisterInputEvents();
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

        private void RegisterInputEvents()
        {
            txtTennv.TextChanged += (s, e) => ClearError(txtTennv);
            txtSdt.TextChanged += (s, e) => ClearError(txtSdt);
            txtEmail.TextChanged += (s, e) => ClearError(txtEmail);

            cboGioiTinh.SelectionChanged += (s, e) => ClearError(cboGioiTinh);
            cboChucVu.SelectionChanged += (s, e) => ClearError(cboChucVu);
            cboTrangThai.SelectionChanged += (s, e) => ClearError(cboTrangThai);
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

        private bool ValidateInputs()
        {
            bool isValid = true;
            Control firstErrorControl = null;

            string namePattern = @"^[\p{L}\s]+$";
            if (string.IsNullOrWhiteSpace(txtTennv.Text) || !Regex.IsMatch(txtTennv.Text, namePattern))
            {
                SetError(txtTennv, "Họ tên không được để trống, không chứa số hoặc ký tự đặc biệt.");
                if (firstErrorControl == null) firstErrorControl = txtTennv;
                isValid = false;
            }

            string phonePattern = @"^0\d{9}$";
            if (string.IsNullOrWhiteSpace(txtSdt.Text) || !Regex.IsMatch(txtSdt.Text, phonePattern))
            {
                SetError(txtSdt, "Số điện thoại phải bắt đầu bằng 0 và đủ 10 chữ số.");
                if (firstErrorControl == null) firstErrorControl = txtSdt;
                isValid = false;
            }

            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (string.IsNullOrWhiteSpace(txtEmail.Text) || !Regex.IsMatch(txtEmail.Text, emailPattern))
            {
                SetError(txtEmail, "Email không đúng định dạng (ví dụ: abc@domain.com).");
                if (firstErrorControl == null) firstErrorControl = txtEmail;
                isValid = false;
            }

            if (cboGioiTinh.SelectedValue == null)
            {
                SetError(cboGioiTinh, "Vui lòng chọn giới tính.");
                if (firstErrorControl == null) firstErrorControl = cboGioiTinh;
                isValid = false;
            }

            if (cboChucVu.SelectedValue == null)
            {
                SetError(cboChucVu, "Vui lòng chọn chức vụ.");
                if (firstErrorControl == null) firstErrorControl = cboChucVu;
                isValid = false;
            }

            if (cboTrangThai.SelectedValue == null)
            {
                SetError(cboTrangThai, "Vui lòng chọn trạng thái.");
                if (firstErrorControl == null) firstErrorControl = cboTrangThai;
                isValid = false;
            }

            if (firstErrorControl != null)
            {
                firstErrorControl.Focus();
            }

            return isValid;
        }

        private void SetError(Control control, string message)
        {
            control.BorderBrush = _errorBrush;
            control.ToolTip = message; // Hiện tooltip khi rê chuột vào
        }

        private void ClearError(Control control)
        {
            control.BorderBrush = _normalBrush;
            control.ToolTip = null;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
            {
                return;
            }

            if (cboTrangThai.SelectedValue != null && int.TryParse(cboTrangThai.SelectedValue.ToString(), out int status))
            {
                CurrentEmployee.TrangThai = status;
            }
            CurrentEmployee.Chucvu = cboChucVu.Text;
            CurrentEmployee.GioiTinh = cboGioiTinh.Text;

            try
            {
                if (_isEditMode)
                {
                    UpdateEmployeeInDatabase();
                    MessageBox.Show("Cập nhật thông tin nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    InsertEmployeeToDatabase();

                    CreateAccountForNewEmployee();

                    MessageBox.Show($"Thêm nhân viên thành công!\nĐã tạo tài khoản mặc định:\n- User: {CurrentEmployee.Manv}\n- Pass: 123456",
                                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateAccountForNewEmployee()
        {

            string sql = @"INSERT INTO TAIKHOAN (MANV, MATKHAU, QUYENHAN, TRANGTHAI) 
                           VALUES (@Manv, '123456', @QuyenHan, 1)";

            var parameters = new SqliteParameter[]
            {
                new SqliteParameter("@Manv", CurrentEmployee.Manv),
                new SqliteParameter("@QuyenHan", CurrentEmployee.Chucvu)
            };

            Database.ExecuteNonQuery(sql, parameters);
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
            string sql = "UPDATE NHANVIEN SET TENNV=@Tennv, CCCD=@Cccd, GIOITINH=@GioiTinh, CHUCVU=@Chucvu, " +
                         "EMAIL=@Email, SDT=@Sdt, DIACHI=@Diachi, NGAYSINH=@Ngaysinh, TRANGTHAI=@TrangThai, AVATAR=@Avatar " +
                         "WHERE MANV=@Manv";
            var parameters = GetParameters();
            Database.ExecuteNonQuery(sql, parameters);
        }

        private SqliteParameter[] GetParameters()
        {
            return new SqliteParameter[]
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
        }
    }
}