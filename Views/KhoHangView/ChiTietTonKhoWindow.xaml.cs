using PharmaDistributionApp.Models; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PharmaDistributionApp.Views.KhoHangView
{
    public partial class ChiTietTonKhoWindow : Window
    {
        private string _maSP;

        public ChiTietTonKhoWindow(string masp)
        {
            InitializeComponent();
            _maSP = masp;
            LoadData();
        }

        public class ChiTietTonKhoItem
        {
            public string TenKho { get; set; }
            public string Malo { get; set; }
            public string HanDung { get; set; }
            public int SoLuong { get; set; }
            public string TrangThai { get; set; }
            public string MauNen { get; set; }
            public string MauChu { get; set; }
        }

        private void LoadData()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var sp = context.Sanphams.FirstOrDefault(s => s.Masp == _maSP);
                    if (sp != null)
                    {
                        txtMaSP.Text = sp.Masp;
                        txtTenSP.Text = sp.Tensp;
                        txtDVT.Text = sp.Dvt;
                    }

    
                    var rawData = (from tk in context.Tonkhos
                                   where tk.Masp == _maSP
                                   join k in context.Khos on tk.Makho equals k.Makho
                                   join lh in context.Lohangs on tk.Malo equals lh.Malo into lhGroup
                                   from subLh in lhGroup.DefaultIfEmpty() 
                                   select new
                                   {
                                       MaLo = tk.Malo,
                                       KhoName = k.Tenkho,
                                       HanDung = subLh != null ? subLh.Hsd : null,
                                       SoLuong = tk.Soluongton
                                   }).ToList();

                    int tongTon = rawData.Sum(x => x.SoLuong);
                    txtTongTon.Text = tongTon.ToString("N0");

                    var listHienThi = new List<ChiTietTonKhoItem>();
                    var homNay = DateOnly.FromDateTime(DateTime.Now);

                    foreach (var item in rawData)
                    {
                        string tt, bg, fg;
                        string hsdStr = item.HanDung.HasValue ? item.HanDung.Value.ToString("dd/MM/yyyy") : "---";

                        if (item.SoLuong == 0)
                        {
                            tt = "Hết hàng";
                            bg = "#FFEBEE"; fg = "#C62828"; 
                        }

                        else if (item.HanDung.HasValue && item.HanDung.Value < homNay)
                        {
                            tt = "Đã hết hạn";
                            bg = "#263238"; fg = "#FF5252"; 
                        }
                        else if (item.HanDung.HasValue && item.HanDung.Value <= homNay.AddDays(30))
                        {
                            tt = "Sắp hết hạn";
                            bg = "#FBE9E7"; fg = "#D84315"; 
                        }
                        else if (item.SoLuong <= 10)
                        {
                            tt = "Sắp hết hàng";
                            bg = "#FFF3E0"; fg = "#EF6C00"; 
                        }
                        else
                        {
                            tt = "Còn hàng";
                            bg = "#E8F5E9"; fg = "#2E7D32";
                        }

                        listHienThi.Add(new ChiTietTonKhoItem
                        {
                            TenKho = item.KhoName,
                            Malo = item.MaLo,
                            HanDung = hsdStr,
                            SoLuong = item.SoLuong,
                            TrangThai = tt,
                            MauNen = bg,
                            MauChu = fg
                        });
                    }

                    dgvChiTietTon.ItemsSource = listHienThi.OrderByDescending(x => x.SoLuong).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải chi tiết: " + ex.Message);
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!dgvChiTietTon.IsMouseOver)
            {
                dgvChiTietTon.SelectedItem = null;
                Keyboard.ClearFocus();
            }
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}