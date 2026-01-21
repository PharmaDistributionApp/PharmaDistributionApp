using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views.LoginView
{
    public partial class ChangePasswordControl : UserControl
    {
        private string _currentManv;

        public event EventHandler CloseRequested;

        public ChangePasswordControl(string manv)
        {
            InitializeComponent();
            _currentManv = manv;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string oldPass = pbOldPass.Password;
            string newPass = pbNewPass.Password;
            string confirmPass = pbConfirmPass.Password;

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

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var tk = context.Taikhoans.FirstOrDefault(x => x.Manv == _currentManv);

                    if (tk == null)
                    {
                        MessageBox.Show("Không tìm thấy tài khoản!", "Lỗi");
                        return;
                    }

                    if (tk.Matkhau != oldPass)
                    {
                        MessageBox.Show("Mật khẩu hiện tại không đúng!", "Lỗi");
                        return;
                    }

                    tk.Matkhau = newPass;
                    context.SaveChanges();

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