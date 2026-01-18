using PharmaDistributionApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class ChiTietPhieuXuatWindow : Window
    {
        private string _mapx;

        public ChiTietPhieuXuatWindow(string mapx)
        {
            InitializeComponent();
            this._mapx = mapx;
            LoadChiTiet();
        }

        private void LoadChiTiet()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // 1. Lấy thông tin phiếu xuất
                    var px = context.Phieuxuats.FirstOrDefault(p => p.Mapx == _mapx);
                    if (px == null) return;

                    txtTieuDe.Text = "PHIẾU XUẤT KHO: " + px.Mapx;
                    txtNgayLap.Text = px.Ngayxuat;
                    txtTrangThai.Text = px.Trangthai;
                    txtSoHD.Text = px.Sohdxuat;

                    // Lấy tên kho
                    var kho = context.Khos.FirstOrDefault(k => k.Makho == px.Makho);
                    txtKho.Text = kho != null ? kho.Tenkho : px.Makho;

                    // Lấy tên nhân viên
                    var nv = context.Nhanviens.FirstOrDefault(n => n.Manv == px.Manv);
                    txtNhanVien.Text = nv != null ? nv.Tennv : px.Manv;

                    // 2. Lấy thông tin Hóa đơn xuất để lấy tên Khách hàng
                    var hdx = context.Hoadonxuats.FirstOrDefault(h => h.Sohdxuat == px.Sohdxuat);
                    txtKhachHang.Text = hdx?.Makh ?? "---";

                    // 3. Lấy chi tiết sản phẩm từ CTHDXUAT (vì Phiếu xuất liên kết qua Số HĐ)
                    var query = from ct in context.Cthdxuats
                                join sp in context.Sanphams on ct.Masp equals sp.Masp
                                join lh in context.Lohangs on ct.Malo equals lh.Malo into lhGroup
                                from lh in lhGroup.DefaultIfEmpty()
                                where ct.Sohdxuat == px.Sohdxuat
                                select new
                                {
                                    ct.Masp,
                                    sp.Tensp,
                                    sp.Dvt,
                                    Sohieu = lh != null ? lh.Sohieu : "---",
                                    Hsd = (lh != null && lh.Hsd.HasValue) ? lh.Hsd.Value.ToString("dd/MM/yyyy") : "---",
                                    ct.Soluong,
                                    ct.Dongiaban,
                                    ct.Thanhtien
                                };

                    dgvChiTiet.ItemsSource = query.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải chi tiết: " + ex.Message);
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