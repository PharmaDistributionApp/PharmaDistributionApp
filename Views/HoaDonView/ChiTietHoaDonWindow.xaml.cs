using System.Windows;

namespace PharmaDistributionApp.Views
{
    public partial class ChiTietHoaDonWindow : Window
    {
        // [SỬA LỖI] Thay InvoiceDummy bằng InvoiceViewModel
        public ChiTietHoaDonWindow(InvoiceViewModel invoice)
        {
            InitializeComponent();

            if (invoice != null)
            {
                txbMaHD.Text = invoice.MaHD;

                // [LƯU Ý] Trong ViewModel mới, tên thuộc tính là DoiTac (chứa tên Khách hoặc NCC)
                txbKhachHang.Text = invoice.DoiTac;

                txbTongTien.Text = invoice.TongTienString;
            }
        }
    }
}