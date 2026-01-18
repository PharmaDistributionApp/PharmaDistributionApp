using System;
using System.Linq;
using System.Windows;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views.KhachHangView
{
    public partial class ThemSuaKhachHangWindow : Window
    {
        private bool _isEditMode = false;
        private Khachhang _currentKH;

        // Constructor cho Thêm mới
        public ThemSuaKhachHangWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            txtTitle.Text = "THÊM KHÁCH HÀNG MỚI";
        }

        // Constructor cho Chỉnh sửa
        public ThemSuaKhachHangWindow(Khachhang kh)
        {
            InitializeComponent();
            _isEditMode = true;
            _currentKH = kh;
            txtTitle.Text = "CẬP NHẬT KHÁCH HÀNG";
            LoadDataToUI();
        }

        private void LoadDataToUI()
        {
            txtMaKH.Text = _currentKH.Makh;
            txtMaKH.IsEnabled = false; // Không cho sửa mã
            txtTenKH.Text = _currentKH.Tenkh;
            txtSdt.Text = _currentKH.Sdt;
            txtEmail.Text = _currentKH.Email;
            txtDiaChi.Text = _currentKH.Diachi;
            cbLoaiKH.Text = _currentKH.Loaikh;
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaKH.Text) || string.IsNullOrWhiteSpace(txtTenKH.Text))
            {
                MessageBox.Show("Mã và Tên khách hàng không được để trống!");
                return;
            }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    if (!_isEditMode)
                    {
                        // Kiểm tra trùng mã
                        if (context.Khachhangs.Any(x => x.Makh == txtMaKH.Text))
                        {
                            MessageBox.Show("Mã khách hàng này đã tồn tại!");
                            return;
                        }

                        var newKH = new Khachhang
                        {
                            Makh = txtMaKH.Text,
                            Tenkh = txtTenKH.Text,
                            Sdt = txtSdt.Text,
                            Email = txtEmail.Text,
                            Diachi = txtDiaChi.Text,
                            Loaikh = cbLoaiKH.Text,
                            Doanhso = 0 // Mặc định doanh số bằng 0
                        };
                        context.Khachhangs.Add(newKH);
                    }
                    else
                    {
                        var editKH = context.Khachhangs.FirstOrDefault(x => x.Makh == _currentKH.Makh);
                        if (editKH != null)
                        {
                            editKH.Tenkh = txtTenKH.Text;
                            editKH.Sdt = txtSdt.Text;
                            editKH.Email = txtEmail.Text;
                            editKH.Diachi = txtDiaChi.Text;
                            editKH.Loaikh = cbLoaiKH.Text;
                        }
                    }
                    context.SaveChanges();
                    DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}