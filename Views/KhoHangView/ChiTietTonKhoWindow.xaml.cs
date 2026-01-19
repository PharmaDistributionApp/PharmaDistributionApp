using PharmaDistributionApp.Models; // Đảm bảo dùng đúng namespace Models của bạn
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

        // Constructor nhận vào Mã Sản Phẩm cần xem
        public ChiTietTonKhoWindow(string masp)
        {
            InitializeComponent();
            _maSP = masp;
            LoadData();
        }

        // Class DTO để hiển thị lên lưới
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
                    // 1. Lấy thông tin cơ bản Sản Phẩm (Header)
                    var sp = context.Sanphams.FirstOrDefault(s => s.Masp == _maSP);
                    if (sp != null)
                    {
                        txtMaSP.Text = sp.Masp;
                        txtTenSP.Text = sp.Tensp;
                        txtDVT.Text = sp.Dvt;
                    }

                    // 2. Lấy dữ liệu chi tiết từ 3 bảng: Tonkho - Kho - Lohang
                    // Logic: Tìm tất cả dòng trong Tonkho có Masp trùng khớp
                    var rawData = (from tk in context.Tonkhos
                                   where tk.Masp == _maSP
                                   join k in context.Khos on tk.Makho equals k.Makho
                                   join lh in context.Lohangs on tk.Malo equals lh.Malo into lhGroup
                                   from subLh in lhGroup.DefaultIfEmpty() // Left Join Lô hàng
                                   select new
                                   {
                                       MaLo = tk.Malo,
                                       KhoName = k.Tenkho,
                                       HanDung = subLh != null ? subLh.Hsd : null,
                                       SoLuong = tk.Soluongton
                                   }).ToList();

                    // 3. Tính tổng tồn kho hiển thị lên Header
                    int tongTon = rawData.Sum(x => x.SoLuong);
                    txtTongTon.Text = tongTon.ToString("N0");

                    // 4. Xử lý Logic màu sắc trạng thái cho từng dòng
                    var listHienThi = new List<ChiTietTonKhoItem>();
                    var homNay = DateOnly.FromDateTime(DateTime.Now);

                    foreach (var item in rawData)
                    {
                        string tt, bg, fg;
                        string hsdStr = item.HanDung.HasValue ? item.HanDung.Value.ToString("dd/MM/yyyy") : "---";

                        // 1. Ưu tiên cao nhất: HẾT HÀNG (Tồn kho = 0)
                        if (item.SoLuong == 0)
                        {
                            tt = "Hết hàng";
                            bg = "#FFEBEE"; fg = "#C62828"; // Đỏ nhạt
                        }
                        // 2. Ưu tiên nhì: ĐÃ HẾT HẠN (Dù còn hàng cũng không bán được -> Nguy hiểm)
                        else if (item.HanDung.HasValue && item.HanDung.Value < homNay)
                        {
                            tt = "Đã hết hạn";
                            bg = "#263238"; fg = "#FF5252"; // Nền Đen xám, Chữ Đỏ tươi
                        }
                        // 3. Ưu tiên ba: SẮP HẾT HẠN (Trong vòng 30 ngày tới)
                        else if (item.HanDung.HasValue && item.HanDung.Value <= homNay.AddDays(30))
                        {
                            tt = "Sắp hết hạn";
                            bg = "#FBE9E7"; fg = "#D84315"; // Cam đỏ
                        }
                        else if (item.SoLuong <= 10)
                        {
                            tt = "Sắp hết hàng";
                            bg = "#FFF3E0"; fg = "#EF6C00"; // Cam
                        }
                        else
                        {
                            tt = "Còn hàng";
                            bg = "#E8F5E9"; fg = "#2E7D32"; // Xanh
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

                    // 5. Gán dữ liệu vào lưới (Sắp xếp ưu tiên nơi nào còn hàng nhiều nhất)
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