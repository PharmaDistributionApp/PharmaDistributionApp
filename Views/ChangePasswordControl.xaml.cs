using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views
{
    public partial class ChangePasswordControl : UserControl
    {
        private string _currentManv;

        // Sự kiện để báo cho AccountControl biết là đã xong việc (để đóng cửa sổ này lại)
        public event EventHandler CloseRequested;

        public ChangePasswordControl(string manv)
        {
            InitializeComponent();
            _currentManv = manv;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Báo hiệu đóng cửa sổ
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string oldPass = pbOldPass.Password;
            string newPass = pbNewPass.Password;
            string confirmPass = pbConfirmPass.Password;

            // 1. Kiểm tra nhập liệu
            if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Cảnh báo");
                return;
            }

            if (newPass != confirmPass)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi");
                return;
            }

            // 2. Kiểm tra DB
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // Tìm tài khoản theo Mã NV
                    var tk = context.Taikhoans.FirstOrDefault(x => x.Manv == _currentManv);

                    if (tk == null)
                    {
                        MessageBox.Show("Không tìm thấy tài khoản!", "Lỗi");
                        return;
                    }

                    // Kiểm tra mật khẩu cũ
                    if (tk.Matkhau != oldPass)
                    {
                        MessageBox.Show("Mật khẩu hiện tại không đúng!", "Lỗi");
                        return;
                    }

                    // 3. Lưu mật khẩu mới
                    tk.Matkhau = newPass;
                    context.SaveChanges();

                    MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo");

                    // Đóng cửa sổ
                    CloseRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }
    }
}