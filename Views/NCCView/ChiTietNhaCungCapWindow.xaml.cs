using System.Windows;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views.NCCView
{
    public partial class ChiTietNhaCungCapWindow : Window
    {
        public ChiTietNhaCungCapWindow(Nhacungcap ncc)
        {
            InitializeComponent();

            // Đổ dữ liệu vào các Label
            if (ncc != null)
            {
                lblMaNCC.Text = ncc.Mancc;
                lblTenNCC.Text = ncc.Tenncc;
                lblSdt.Text = string.IsNullOrEmpty(ncc.Sdt) ? "(Chưa có)" : ncc.Sdt;
                lblEmail.Text = string.IsNullOrEmpty(ncc.Email) ? "(Chưa có)" : ncc.Email;
                lblDiaChi.Text = string.IsNullOrEmpty(ncc.Diachi) ? "(Chưa có)" : ncc.Diachi;
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}