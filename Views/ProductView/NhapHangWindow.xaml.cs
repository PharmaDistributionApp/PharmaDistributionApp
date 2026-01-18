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

namespace PharmaDistributionApp.Views.ProductView

{

    public partial class NhapHangWindow : Window

    {

        private enum Mode { Nhap, Xuat }

        private Mode _currentMode = Mode.Nhap;


        public class ChiTietView // Đảm bảo class nằm độc lập ở đây

        {

            public string Masp { get; set; }

            public string Tensp { get; set; }

            public string Dvt { get; set; }

            public string Sohieu { get; set; }

            public string Hsd { get; set; }

            public int Soluong { get; set; }

            public decimal Dongia { get; set; }

            public decimal Thanhtien { get; set; }

            public string Malo { get; set; }

            public string SelectedMakho { get; set; }

        }


        private List<ChiTietView> _listChiTiet = new List<ChiTietView>();


        // [ĐÃ SỬA] Thêm tham số isXuat để biết mở ở chế độ nào

        public NhapHangWindow(bool isXuat = false)

        {

            InitializeComponent();

            dpNgayLap.SelectedDate = DateTime.Now;


            if (isXuat)

            {

                radXuat.IsChecked = true; // Tự check vào Radio Xuất

                _currentMode = Mode.Xuat;

            }

            else

            {

                radNhap.IsChecked = true; // Mặc định là Nhập

                _currentMode = Mode.Nhap;

            }


            // Gọi hàm này để cập nhật giao diện (Tiêu đề, màu sắc, Hint...) theo mode đã chọn

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


            // Reset dữ liệu khi đổi tab

            cboHoaDon.ItemsSource = null;

            dgvChiTiet.ItemsSource = null;

            txtDoiTuong.Text = "---"; txtNgayHD.Text = "---"; txtTongTienHD.Text = "0 đ";

            _listChiTiet.Clear();


            LoadInitData();

        }


        // Tách riêng hàm cập nhật giao diện để dùng chung

        private void UpdateUIMode()

        {

            var color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C70BA"));

            if (_currentMode == Mode.Nhap)

            {

                txtTieuDe.Text = "Tạo phiếu nhập hàng";

                HintAssist.SetHint(cboHoaDon, "Chọn Hóa Đơn Nhập");

                lblDoiTuong.Text = "Nhà cung cấp:";

            }

            else

            {

                txtTieuDe.Text = "Tạo phiếu xuất hàng";

                HintAssist.SetHint(cboHoaDon, "Chọn Hóa Đơn Xuất");

                lblDoiTuong.Text = "Khách hàng:";

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

                    cboKho.ItemsSource = listKho;

                    cboKho.DisplayMemberPath = "Tenkho";

                    cboKho.SelectedValuePath = "Makho";


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


        private void cboKho_SelectionChanged(object sender, SelectionChangedEventArgs e)

        {

            if (cboKho.SelectedValue == null || _listChiTiet == null) return;

            string defaultKho = cboKho.SelectedValue.ToString();

            foreach (var item in _listChiTiet) { item.SelectedMakho = defaultKho; }

            dgvChiTiet.Items.Refresh();

        }


        private void cboHoaDon_SelectionChanged(object sender, SelectionChangedEventArgs e)

        {

            if (cboHoaDon.SelectedValue == null) return;

            string soHD = cboHoaDon.SelectedValue.ToString();


            string defaultKho = "";

            if (cboKho.SelectedValue != null) defaultKho = cboKho.SelectedValue.ToString();

            else

            {

                using (var ctx = new QuanlyphanphoiduocphamContext())

                {

                    var k = ctx.Khos.FirstOrDefault();

                    if (k != null) defaultKho = k.Makho;

                }

            }


            try

            {

                using (var context = new QuanlyphanphoiduocphamContext())

                {

                    if (_currentMode == Mode.Nhap)

                    {

                        var hd = context.Hoadonnhaps.FirstOrDefault(h => h.Sohdnhap == soHD);

                        if (hd != null) { txtDoiTuong.Text = hd.Mancc; txtNgayHD.Text = hd.Ngaylap; txtTongTienHD.Text = string.Format("{0:N0} đ", hd.Tongtien); }


                        _listChiTiet = context.Cthdnhaps.Where(ct => ct.Sohdnhap == soHD)

                            .Include(ct => ct.MaspNavigation).Include(ct => ct.MaloNavigation)

                            .Select(ct => new ChiTietView

                            {

                                Masp = ct.Masp,

                                Tensp = ct.MaspNavigation.Tensp,

                                Dvt = ct.MaspNavigation.Dvt,

                                Sohieu = ct.MaloNavigation.Sohieu,

                                Hsd = ct.MaloNavigation.Hsd != null ? ct.MaloNavigation.Hsd.Value.ToString("dd/MM/yyyy") : "",

                                Soluong = ct.Soluong,

                                Dongia = ct.Dongianhap,

                                Thanhtien = ct.Thanhtien,

                                Malo = ct.Malo,

                                SelectedMakho = defaultKho

                            }).ToList();

                    }

                    else // CHẾ ĐỘ PHIẾU XUẤT

                    {

                        var hd = context.Hoadonxuats.FirstOrDefault(h => h.Sohdxuat == soHD);

                        if (hd != null)

                        {

                            txtDoiTuong.Text = hd.Makh ?? "---";

                            txtNgayHD.Text = hd.Ngaylap ?? "---";

                            txtTongTienHD.Text = string.Format("{0:N0} đ", hd.Tongtien ?? 0);

                        }


                        // Bước 1: Lấy danh sách chi tiết thô từ CTHDXUAT

                        var rawDetails = context.Cthdxuats.Where(ct => ct.Sohdxuat == soHD).ToList();


                        // Bước 2: Duyệt từng dòng để lấy thêm tên SP và thông tin Lô từ các bảng khác

                        _listChiTiet = rawDetails.Select(ct => {

                            // Tìm sản phẩm thủ công để tránh lỗi Navigation

                            var sp = context.Sanphams.FirstOrDefault(s => s.Masp == ct.Masp);

                            // Tìm lô hàng thủ công

                            var lh = context.Lohangs.FirstOrDefault(l => l.Malo == ct.Malo);


                            return new ChiTietView

                            {

                                Masp = ct.Masp,

                                Tensp = sp?.Tensp ?? "Không xác định",

                                Dvt = sp?.Dvt ?? "",

                                Sohieu = lh?.Sohieu ?? "---",

                                // Chuyển DateOnly? từ SQLite sang string an toàn

                                Hsd = lh?.Hsd != null ? lh.Hsd.Value.ToString("dd/MM/yyyy") : "---",

                                Soluong = ct.Soluong,

                                Dongia = ct.Dongiaban,

                                Thanhtien = ct.Thanhtien,

                                Malo = ct.Malo,

                                SelectedMakho = defaultKho

                            };

                        }).ToList();

                    }

                    // Gán dữ liệu vào bảng (dgvChiTiet)

                    dgvChiTiet.ItemsSource = null;

                    dgvChiTiet.ItemsSource = _listChiTiet;

                }

            }

            catch (Exception ex)

            {

                MessageBox.Show("Lỗi load chi tiết: " + ex.Message);

            }

        }


        private void BtnLuu_Click(object sender, RoutedEventArgs e)

        {

            if (cboHoaDon.SelectedValue == null) { MessageBox.Show("Chưa chọn hóa đơn!"); return; }

            string soHD = cboHoaDon.SelectedValue.ToString();

            string khoDaiDien = cboKho.SelectedValue != null ? cboKho.SelectedValue.ToString() : (_listChiTiet.Count > 0 ? _listChiTiet[0].SelectedMakho : "");

            string ngay = dpNgayLap.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");


            try

            {

                using (var context = new QuanlyphanphoiduocphamContext())

                {

                    if (_currentMode == Mode.Nhap)

                    {

                        var pn = new Phieunhap

                        {

                            Mapn = "PN" + DateTime.Now.ToString("yyMMddHHmm"),

                            Sohdnhap = soHD,

                            Makho = khoDaiDien,

                            Ngaynhap = ngay,

                            Manv = "NV01",

                            Trangthai = "Chờ duyệt"

                        };

                        context.Phieunhaps.Add(pn);

                    }

                    else

                    {

                        var px = new Phieuxuat

                        {

                            Mapx = "PX" + DateTime.Now.ToString("yyMMddHHmm"),

                            Sohdxuat = soHD,

                            Makho = khoDaiDien,

                            Ngayxuat = ngay,

                            Manv = "NV01",

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