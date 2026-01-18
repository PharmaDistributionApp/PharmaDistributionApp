using System.Windows;
using PharmaDistributionApp.Models;
using System.Globalization;

namespace PharmaDistributionApp.Views.KhachHangView
{
    public partial class ChiTietKhachHang : Window
    {
        public ChiTietKhachHang(Khachhang kh)
        {
            InitializeComponent();
            LoadData(kh);
        }

        private void LoadData(Khachhang kh)
        {
            if (kh != null)
            {
                lblMaKH.Text = kh.Makh;
                lblTenKH.Text = kh.Tenkh;
                lblSdt.Text = kh.Sdt ?? "Chưa cập nhật";
                lblEmail.Text = kh.Email ?? "Chưa cập nhật";
                lblDiaChi.Text = kh.Diachi ?? "Chưa cập nhật";
                lblLoaiKH.Text = kh.Loaikh ?? "Khách lẻ";

                // Định dạng tiền tệ cho Doanh số
                decimal doanhSo = kh.Doanhso ?? 0;
                lblDoanhSo.Text = doanhSo.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " VNĐ";
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}