using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using PharmaDistributionApp.Models;
using MaterialDesignThemes.Wpf;
using PharmaDistributionApp.Services;

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class NhapHangWindow : Window
    {
        private enum Mode { Nhap, Xuat }
        private Mode _currentMode = Mode.Nhap;

        public class ChiTietView
        {
            public string Masp { get; set; }
            public string Tensp { get; set; }
            public string Dvt { get; set; }
            public string Malo { get; set; }
            public string Hsd { get; set; }
            public int Soluong { get; set; }
            public decimal Dongia { get; set; } 
            public decimal Thanhtien { get; set; }
            public string SelectedMakho { get; set; }
        }

        private List<ChiTietView> _listChiTiet = new List<ChiTietView>();

        public NhapHangWindow(bool isXuat = false)
        {
            InitializeComponent();
            dpNgayLap.SelectedDate = DateTime.Now;

            if (isXuat)
            {
                radXuat.IsChecked = true;
                _currentMode = Mode.Xuat;
            }
            else
            {
                radNhap.IsChecked = true;
                _currentMode = Mode.Nhap;
            }

            UpdateUIMode();
            LoadInitData();
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!dgvChiTiet.IsMouseOver) dgvChiTiet.UnselectAll();
        }

        private void Mode_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = radNhap.IsChecked == true ? Mode.Nhap : Mode.Xuat;
            UpdateUIMode();

            cboHoaDon.ItemsSource = null;
            dgvChiTiet.ItemsSource = null;
            txtKhachHang.Text = "---"; txtNgayHD.Text = "---"; txtTongTienHD.Text = "0 đ";
            _listChiTiet.Clear();

            LoadInitData();
        }

        private void UpdateUIMode()
        {
            var color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C70BA"));


            if (UserSession.CurrentUser != null)
                txtNguoiLap.Text = UserSession.CurrentUser.Tennv;
            else
                txtNguoiLap.Text = "Admin (Test)";

            if (_currentMode == Mode.Nhap)
            {
                txtTieuDe.Text = "Tạo phiếu nhập hàng";
                HintAssist.SetHint(cboHoaDon, "Chọn Hóa Đơn Nhập");

                lblKhachHang.Visibility = Visibility.Collapsed;
                txtKhachHang.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtTieuDe.Text = "Tạo phiếu xuất hàng";
                HintAssist.SetHint(cboHoaDon, "Chọn Hóa Đơn Xuất");
                lblKhachHang.Visibility = Visibility.Visible;
                txtKhachHang.Visibility = Visibility.Visible;
                lblKhachHang.Text = "Khách hàng:"; 
            }

            txtTieuDe.Foreground = color;
            btnLuu.Background = color;
        }

        private void LoadInitData()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    
                    var listKho = context.Khos.ToList();
                    CollectionViewSource cvs = (CollectionViewSource)this.Resources["cvsKhos"];
                    cvs.Source = listKho;

                    if (_currentMode == Mode.Nhap)
                    {
                        var list = context.Hoadonnhaps
                            .Where(h => !context.Phieunhaps.Any(p => p.Sohdnhap == h.Sohdnhap))
                            .Select(h => new { Ma = h.Sohdnhap, HienThi = h.Sohdnhap }).ToList();
                        cboHoaDon.ItemsSource = list;
                    }
                    else
                    {
                        var list = context.Hoadonxuats
                            .Where(h => !context.Phieuxuats.Any(p => p.Sohdxuat == h.Sohdxuat))
                            .Select(h => new { Ma = h.Sohdxuat, HienThi = h.Sohdxuat }).ToList();
                        cboHoaDon.ItemsSource = list;
                    }

                    cboHoaDon.DisplayMemberPath = "HienThi";
                    cboHoaDon.SelectedValuePath = "Ma";
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void cboHoaDon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboHoaDon.SelectedValue == null) return;
            string soHD = cboHoaDon.SelectedValue.ToString();
            string defaultKho = "";
            try
            {
                using (var ctx = new QuanlyphanphoiduocphamContext())
                {
                    var k = ctx.Khos.FirstOrDefault();
                    if (k != null) defaultKho = k.Makho;
                }
            }
            catch { }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    if (_currentMode == Mode.Nhap)
                    {
                        var hd = context.Hoadonnhaps.FirstOrDefault(h => h.Sohdnhap == soHD);
                        if (hd != null) { txtKhachHang.Text = hd.Mancc; txtNgayHD.Text = hd.Ngaylap; txtTongTienHD.Text = string.Format("{0:N0} đ", hd.Tongtien); }

                        _listChiTiet = context.Cthdnhaps.Where(ct => ct.Sohdnhap == soHD)
                            .Include(ct => ct.MaspNavigation).Include(ct => ct.MaloNavigation)
                            .Select(ct => new ChiTietView
                            {
                                Masp = ct.Masp,
                                Tensp = ct.MaspNavigation.Tensp,
                                Dvt = ct.MaspNavigation.Dvt,
                                Malo = ct.Malo, 
                                Hsd = ct.MaloNavigation.Hsd != null ? ct.MaloNavigation.Hsd.Value.ToString("dd/MM/yyyy") : "",
                                Soluong = ct.Soluong,
                                Dongia = ct.Dongianhap,
                                Thanhtien = ct.Thanhtien,
                                SelectedMakho = defaultKho
                            }).ToList();
                    }
                    else // CHẾ ĐỘ PHIẾU XUẤT
                    {
                        var hd = context.Hoadonxuats.FirstOrDefault(h => h.Sohdxuat == soHD);
                        if (hd != null)
                        {
                            txtKhachHang.Text = hd.Makh ?? "---";
                            txtNgayHD.Text = hd.Ngaylap ?? "---";
                            txtTongTienHD.Text = string.Format("{0:N0} đ", hd.Tongtien ?? 0);
                        }

                        var rawDetails = context.Cthdxuats.Where(ct => ct.Sohdxuat == soHD).ToList();
                        _listChiTiet = rawDetails.Select(ct => {
                            var sp = context.Sanphams.FirstOrDefault(s => s.Masp == ct.Masp);
                            var lh = context.Lohangs.FirstOrDefault(l => l.Malo == ct.Malo);
                            return new ChiTietView
                            {
                                Masp = ct.Masp,
                                Tensp = sp?.Tensp ?? "Không xác định",
                                Dvt = sp?.Dvt ?? "",
                                Malo = ct.Malo,
                                Hsd = lh?.Hsd != null ? lh.Hsd.Value.ToString("dd/MM/yyyy") : "---",
                                Soluong = ct.Soluong,
                                Dongia = ct.Dongiaban,
                                Thanhtien = ct.Thanhtien,
                                SelectedMakho = defaultKho
                            };
                        }).ToList();
                    }
                    dgvChiTiet.ItemsSource = null;
                    dgvChiTiet.ItemsSource = _listChiTiet;
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi load chi tiết: " + ex.Message); }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (cboHoaDon.SelectedValue == null) { MessageBox.Show("Chưa chọn hóa đơn!"); return; }
            string soHD = cboHoaDon.SelectedValue.ToString();
            string ngay = dpNgayLap.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
            string khoDaiDien = (_listChiTiet.Count > 0) ? _listChiTiet[0].SelectedMakho : "";

            string maNhanVien = "NV01";
            if (UserSession.CurrentUser != null)
            {
                maNhanVien = UserSession.CurrentUser.Manv;
            }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    if (_currentMode == Mode.Nhap)
                    {
                        // Logic tạo mã PN tự động
                        var danhSachMa = context.Phieunhaps.Select(x => x.Mapn).ToList();
                        int maxNum = 0;
                        foreach (var ma in danhSachMa)
                        {
                            if (!string.IsNullOrEmpty(ma) && ma.StartsWith("PN") && int.TryParse(ma.Substring(2), out int num))
                            {
                                if (num > maxNum) maxNum = num;
                            }
                        }
                        string newID = "PN" + (maxNum + 1).ToString("D3");

                        var pn = new Phieunhap
                        {
                            Mapn = newID,
                            Sohdnhap = soHD,
                            Makho = khoDaiDien,
                            Ngaynhap = ngay,
                            Manv = maNhanVien,
                            Trangthai = "Chờ duyệt"
                        };
                        context.Phieunhaps.Add(pn);
                    }
                    else 
                    {
                        var danhSachMa = context.Phieuxuats.Select(x => x.Mapx).ToList();
                        int maxNum = 0;
                        foreach (var ma in danhSachMa)
                        {
                            if (!string.IsNullOrEmpty(ma) && ma.StartsWith("PX") && int.TryParse(ma.Substring(2), out int num))
                            {
                                if (num > maxNum) maxNum = num;
                            }
                        }
                        string newID = "PX" + (maxNum + 1).ToString("D3");

                        var px = new Phieuxuat
                        {
                            Mapx = newID,
                            Sohdxuat = soHD,
                            Makho = khoDaiDien,
                            Ngayxuat = ngay,
                            Manv = maNhanVien,
                            Trangthai = "Chờ duyệt"
                        };
                        context.Phieuxuats.Add(px);
                    }

                    context.SaveChanges();
                    MessageBox.Show("Tạo phiếu thành công!");
                    this.Close();
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e) { this.Close(); }
    }
}