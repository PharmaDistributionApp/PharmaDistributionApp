using PharmaDistributionApp.Models;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media; // Bắt buộc có để dùng BrushConverter

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class ChiTietSanPhamWindow : Window
    {
        private string _maSp;

        public ChiTietSanPhamWindow(string maSp)
        {
            InitializeComponent();
            _maSp = maSp;
            LoadChiTiet();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        private void LoadChiTiet()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var sp = context.Sanphams.FirstOrDefault(x => x.Masp == _maSp);

                    if (sp != null)
                    {
                        string tenLoai = "Chưa phân loại";
                        if (!string.IsNullOrEmpty(sp.Maloai))
                        {
                            var loai = context.Loaisps.FirstOrDefault(x => x.Maloai == sp.Maloai);
                            if (loai != null) tenLoai = loai.Tenloai;
                        }
                        var tongTon = context.Tonkhos
                                             .Where(t => t.Masp == _maSp)
                                             .Sum(t => (int?)t.Soluongton) ?? 0;
                        lblTenSP.Text = sp.Tensp;
                        txtMaSP.Text = sp.Masp;
                        txtLoai.Text = tenLoai;
                        txtDVT.Text = sp.Dvt;
                        txtNuocSX.Text = sp.Nuocsx ?? "Chưa cập nhật";
                        txtNhaCungCap.Text = sp.Nhacungcap ?? "Chưa cập nhật";
                        txtGiaBan.Text = string.Format("{0:N0} đ", sp.Giaban);

                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi hiển thị chi tiết: " + ex.Message);
            }
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}