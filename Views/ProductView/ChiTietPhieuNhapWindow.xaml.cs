using Microsoft.EntityFrameworkCore; // Nhớ thêm dòng này
using PharmaDistributionApp.Models;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class ChiTietPhieuNhapWindow : Window
    {
        public ChiTietPhieuNhapWindow(string maPhieuNhap)
        {
            InitializeComponent();
            LoadData(maPhieuNhap);
        }

        private void LoadData(string maPN)
        {
            using (var context = new QuanlyphanphoiduocphamContext())
            {
                // 1. Lấy thông tin phiếu nhập
                var phieu = context.Phieunhaps.FirstOrDefault(p => p.Mapn == maPN);
                if (phieu == null) return;

                txtTieuDe.Text = "PHIẾU NHẬP KHO: " + phieu.Mapn;
                txtNgayLap.Text = phieu.Ngaynhap; // Hoặc .ToString("dd/MM/yyyy") nếu kiểu DateTime
                txtTrangThai.Text = phieu.Trangthai;
                txtSoHD.Text = phieu.Sohdnhap;

                // Lấy tên Kho
                var kho = context.Khos.FirstOrDefault(k => k.Makho == phieu.Makho);
                txtKho.Text = kho != null ? kho.Tenkho : phieu.Makho;

                // Lấy thông tin từ Hóa đơn gốc (NCC, v.v...)
                var hd = context.Hoadonnhaps.FirstOrDefault(h => h.Sohdnhap == phieu.Sohdnhap);
                if (hd != null)
                {
                    var ncc = context.Nhacungcaps.FirstOrDefault(n => n.Mancc == hd.Mancc);
                    txtNCC.Text = ncc != null ? ncc.Tenncc : hd.Mancc;
                }

                // 2. Lấy chi tiết sản phẩm (dựa vào số hóa đơn)
                var listChiTiet = context.Cthdnhaps
                    .Include(ct => ct.MaspNavigation)
                    .Include(ct => ct.MaloNavigation)
                    .Where(ct => ct.Sohdnhap == phieu.Sohdnhap)
                    .Select(ct => new
                    {
                        ct.Masp,
                        ct.MaspNavigation.Tensp,
                        ct.MaspNavigation.Dvt,
                        ct.MaloNavigation.Sohieu,
                        Hsd = ct.MaloNavigation.Hsd.ToString(),
                        ct.Soluong,
                        ct.Dongianhap,
                        ct.Thanhtien
                    })
                    .ToList();

                dgvChiTiet.ItemsSource = listChiTiet;
            }
        }
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Bỏ chọn dòng hiện tại trong DataGrid
            dgvChiTiet.UnselectAll();

            // Xóa focus vật lý khỏi DataGrid để mất các viền focus (nếu còn)
            Keyboard.ClearFocus();
        }
        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}