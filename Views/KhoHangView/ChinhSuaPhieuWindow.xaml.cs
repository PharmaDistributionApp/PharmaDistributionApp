using Microsoft.EntityFrameworkCore;
using PharmaDistributionApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PharmaDistributionApp.Views.KhoHangView
{
    public partial class ChinhSuaPhieuWindow : Window
    {
        private string _maPhieu;
        private bool _isXuat;
        private ObservableCollection<ChiTietPhieuEditItem> _listChiTiet;
        public class ChiTietPhieuEditItem
        {
            public string Masp { get; set; }
            public string Tensp { get; set; }
            public string Dvt { get; set; }
            public string Malo { get; set; }
            public int Soluong { get; set; }   
            public decimal Dongia { get; set; }  

            public decimal Thanhtien => (decimal)Soluong * Dongia;
        }
        public ChinhSuaPhieuWindow(string maPhieu, bool isXuat)
        {
            InitializeComponent();
            _maPhieu = maPhieu;
            _isXuat = isXuat;
            LoadData();
            this.GotFocus += (s, e) =>
            {
                if (!(e.OriginalSource is DataGrid) && !(e.OriginalSource is DataGridCell))
                {
                    dgvChiTiet.UnselectAll(); 
                }
            };
        }

        private void LoadData()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    txtMaPhieu.Text = _maPhieu;

                    if (!_isXuat) 
                    {
                        txtTieuDe.Text = "CHỈNH SỬA PHIẾU NHẬP";
                        lblDoiTuong.Visibility = Visibility.Collapsed;
                        txtDoiTuong.Visibility = Visibility.Collapsed;

                        var phieu = context.Phieunhaps.FirstOrDefault(p => p.Mapn == _maPhieu);
                        if (phieu != null)
                        {
                            cboTrangThai.Text = phieu.Trangthai;
                            dpNgayLap.SelectedDate = DateTime.Parse(phieu.Ngaynhap);

                            var queryNhap = from ct in context.Cthdnhaps
                                            join sp in context.Sanphams on ct.Masp equals sp.Masp
                                            where ct.Sohdnhap == phieu.Sohdnhap
                                            select new ChiTietPhieuEditItem
                                            {
                                                Masp = ct.Masp,
                                                Tensp = sp.Tensp,
                                                Dvt = sp.Dvt,
                                                Malo = ct.Malo,
                                                Soluong = ct.Soluong,     
                                                Dongia = ct.Dongianhap  
                                            };
                            _listChiTiet = new ObservableCollection<ChiTietPhieuEditItem>(queryNhap.ToList());
                        }
                    }
                    else
                    {
                        txtTieuDe.Text = "CHỈNH SỬA PHIẾU XUẤT";
                        lblDoiTuong.Text = "Khách hàng:";

                        var phieu = context.Phieuxuats.FirstOrDefault(p => p.Mapx == _maPhieu);
                        if (phieu != null)
                        {
                            cboTrangThai.Text = phieu.Trangthai;
                            dpNgayLap.SelectedDate = DateTime.Parse(phieu.Ngayxuat);

                            var hd = context.Hoadonxuats.FirstOrDefault(h => h.Sohdxuat == phieu.Sohdxuat);
                            txtDoiTuong.Text = hd?.Makh ?? "---";

                            var queryXuat = from ct in context.Cthdxuats
                                            join sp in context.Sanphams on ct.Masp equals sp.Masp
                                            where ct.Sohdxuat == phieu.Sohdxuat
                                            select new ChiTietPhieuEditItem
                                            {
                                                Masp = ct.Masp,
                                                Tensp = sp.Tensp,
                                                Dvt = sp.Dvt,
                                                Malo = ct.Malo,
                                                Soluong = ct.Soluong,    
                                                Dongia = ct.Dongiaban     
                                            };
                            _listChiTiet = new ObservableCollection<ChiTietPhieuEditItem>(queryXuat.ToList());
                        }
                    }

                    dgvChiTiet.ItemsSource = _listChiTiet;
                    TinhTongTien();
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message); }
        }

        private void TinhTongTien()
        {
            if (_listChiTiet == null) return;
            decimal tong = _listChiTiet.Sum(x => x.Thanhtien);
            txtTongTien.Text = string.Format("{0:N0} đ", tong);
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    if (!_isXuat)   
                    {
                        var phieu = context.Phieunhaps.FirstOrDefault(p => p.Mapn == _maPhieu);
                        if (phieu != null)
                        {
                            phieu.Trangthai = cboTrangThai.Text;
                            phieu.Ngaynhap = dpNgayLap.SelectedDate?.ToString("yyyy-MM-dd");
                            var oldDetails = context.Cthdnhaps.Where(c => c.Sohdnhap == phieu.Sohdnhap);
                            context.Cthdnhaps.RemoveRange(oldDetails);
                            foreach (var item in _listChiTiet)
                            {
                                context.Cthdnhaps.Add(new Cthdnhap
                                {
                                    Sohdnhap = phieu.Sohdnhap,
                                    Masp = item.Masp,
                                    Malo = item.Malo,
                                    Soluong = item.Soluong,
                                    Dongianhap = item.Dongia, 
                                    Thanhtien = item.Thanhtien
                                });
                            }
                            var hd = context.Hoadonnhaps.FirstOrDefault(h => h.Sohdnhap == phieu.Sohdnhap);
                            if (hd != null) hd.Tongtien = (double)_listChiTiet.Sum(x => x.Thanhtien);
                        }
                    }
                    else 
                    {
                        var phieu = context.Phieuxuats.FirstOrDefault(p => p.Mapx == _maPhieu);
                        if (phieu != null)
                        {
                            phieu.Trangthai = cboTrangThai.Text;
                            phieu.Ngayxuat = dpNgayLap.SelectedDate?.ToString("yyyy-MM-dd");
                            var oldDetails = context.Cthdxuats.Where(c => c.Sohdxuat == phieu.Sohdxuat);
                            context.Cthdxuats.RemoveRange(oldDetails);
                            foreach (var item in _listChiTiet)
                            {
                                context.Cthdxuats.Add(new Cthdxuat
                                {
                                    Sohdxuat = phieu.Sohdxuat,
                                    Masp = item.Masp,
                                    Malo = item.Malo,
                                    Soluong = item.Soluong,
                                    Dongiaban = item.Dongia, 
                                    Thanhtien = item.Thanhtien
                                });
                            }
                            var hd = context.Hoadonxuats.FirstOrDefault(h => h.Sohdxuat == phieu.Sohdxuat);
                            if (hd != null)
                            {
                                hd.Makh = txtDoiTuong.Text;
                                hd.Tongtien = (double)_listChiTiet.Sum(x => x.Thanhtien);
                            }
                        }
                    }
                    context.SaveChanges();

                    MessageBox.Show("Đã cập nhật phiếu và chi tiết thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message, "Lỗi nghiêm trọng", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dgvChiTiet.UnselectAll(); 
            Keyboard.ClearFocus();
        }
        private void dgvChiTiet_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                TinhTongTien();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        private void BtnXoaDong_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.DataContext is ChiTietPhieuEditItem itemXoa)
            {
                var result = MessageBox.Show($"Xóa thuốc {itemXoa.Tensp}?", "Xác nhận",
                                           MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _listChiTiet.Remove(itemXoa);
                    TinhTongTien();
                }
            }
        }
        private void BtnHuy_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}