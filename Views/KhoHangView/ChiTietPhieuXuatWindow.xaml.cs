using Microsoft.EntityFrameworkCore;
using PharmaDistributionApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PharmaDistributionApp.Services;

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
                    var px = context.Phieuxuats.FirstOrDefault(p => p.Mapx == _mapx);
                    if (px == null) return;

                    txtTieuDe.Text = "PHIẾU XUẤT KHO: " + px.Mapx;
                    txtNgayLap.Text = px.Ngayxuat;
                    txtTrangThai.Text = px.Trangthai;
                    txtSoHD.Text = px.Sohdxuat;

                    var kho = context.Khos.FirstOrDefault(k => k.Makho == px.Makho);
                    txtKho.Text = kho != null ? kho.Tenkho : px.Makho;

                    var nv = context.Nhanviens.FirstOrDefault(n => n.Manv == px.Manv);
                    txtNhanVien.Text = nv != null ? nv.Tennv : px.Manv;

                    var hdx = context.Hoadonxuats.FirstOrDefault(h => h.Sohdxuat == px.Sohdxuat);
                    txtKhachHang.Text = hdx?.Makh ?? "---";

                    bool coQuyen = false;
                    if (UserSession.CurrentUser != null)
                    {
                        string chucVu = UserSession.CurrentUser.Chucvu; 
                        string[] cacSep = { "Admin", "Giám đốc", "Quản lý kho" };
                        coQuyen = cacSep.Contains(chucVu);
                    }

                    bool dangChoDuyet = px.Trangthai == "Chờ duyệt";

                    if (coQuyen && dangChoDuyet)
                    {
                        btnPheDuyet.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        btnPheDuyet.Visibility = Visibility.Collapsed;
                    }

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
                                    ct.Malo,
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
            dgvChiTiet.UnselectAll();
            Keyboard.ClearFocus();
        }

        private void BtnPheDuyet_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Xác nhận duyệt Phiếu Xuất này?\n(Kho sẽ bị TRỪ số lượng)",
                "Phê duyệt", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            string trangThaiMoi = "";
            if (result == MessageBoxResult.Yes) trangThaiMoi = "Đã duyệt";
            else if (result == MessageBoxResult.No) trangThaiMoi = "Đã hủy";
            else return;

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var px = context.Phieuxuats.FirstOrDefault(p => p.Mapx == _mapx);
                    if (px != null)
                    {
                        if (px.Trangthai == "Đã duyệt" || px.Trangthai == "Đã hủy")
                        {
                            MessageBox.Show("Phiếu này đã được xử lý rồi!", "Cảnh báo");
                            return;
                        }

                        var listChiTiet = context.Cthdxuats.Where(ct => ct.Sohdxuat == px.Sohdxuat).ToList();

                        if (listChiTiet.Count == 0 && trangThaiMoi == "Đã duyệt")
                        {
                            MessageBox.Show("Lỗi: Không tìm thấy chi tiết sản phẩm!", "Lỗi dữ liệu");
                            return;
                        }

                        if (trangThaiMoi == "Đã duyệt")
                        {
                            foreach (var item in listChiTiet)
                            {
                                var tonKho = context.Tonkhos.FirstOrDefault(t =>
                                    t.Masp == item.Masp && t.Malo == item.Malo && t.Makho == px.Makho);

                                if (tonKho == null || tonKho.Soluongton < item.Soluong)
                                {
                                    MessageBox.Show($"Lỗi: Không đủ hàng!\nSP: {item.Masp} - Lô: {item.Malo}\nTồn: {tonKho?.Soluongton ?? 0} < Cần: {item.Soluong}", "Lỗi");
                                    return;
                                }
                            }

                            foreach (var item in listChiTiet)
                            {
                                var tonKho = context.Tonkhos.FirstOrDefault(t =>
                                    t.Masp == item.Masp && t.Malo == item.Malo && t.Makho == px.Makho);

                                if (tonKho != null)
                                {
                                    tonKho.Soluongton -= item.Soluong;
                                    if (tonKho.Soluongton == 0)
                                    {
                                        context.Tonkhos.Remove(tonKho);
                                    }
                                    else
                                    {
                                        context.Entry(tonKho).State = EntityState.Modified;
                                    }
                                }
                            }
                        }

                        else if (trangThaiMoi == "Đã hủy")
                        {

                        }

                        px.Trangthai = trangThaiMoi;
                        context.Entry(px).State = EntityState.Modified;

                        context.SaveChanges();

                        MessageBox.Show($"Đã {trangThaiMoi} phiếu thành công!", "Thông báo");
                        LoadChiTiet();
                    }
                }
            }
            catch (Exception ex)
            {
               
                string msg = ex.Message;
                if (ex.InnerException != null) msg += "\nChi tiết: " + ex.InnerException.Message;
                MessageBox.Show("Lỗi cập nhật: " + msg);
            }
        }
        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}