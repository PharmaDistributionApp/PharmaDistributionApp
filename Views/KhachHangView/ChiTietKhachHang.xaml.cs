using PharmaDistributionApp.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PharmaDistributionApp.Views.KhachHangView
{
    public partial class ChiTietKhachHang : Window
    {
        public ChiTietKhachHang(Khachhang kh)
        {
            InitializeComponent();
            SetupEvents();
            LoadData(kh);
        }

        private void SetupEvents()
        {
            this.KeyDown += (s, e) => {
                if (e.Key == Key.Escape) this.Close();
            };
        }

        private void LoadData(Khachhang kh)
        {
            if (kh == null) return;

            lblMaKH.Text = kh.Makh;
            lblTenKH.Text = kh.Tenkh?.ToUpper();
            lblSdt.Text = !string.IsNullOrEmpty(kh.Sdt) ? kh.Sdt : "---";
            lblEmail.Text = !string.IsNullOrEmpty(kh.Email) ? kh.Email : "---";
            lblDiaChi.Text = !string.IsNullOrEmpty(kh.Diachi) ? kh.Diachi : "---";
            lblLoaiKH.Text = !string.IsNullOrEmpty(kh.Loaikh) ? kh.Loaikh : "Thường";

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var listHoadon = context.Hoadonxuats
                        .Where(hd => hd.Makh == kh.Makh)
                        .OrderByDescending(hd => hd.Ngaylap)
                        .ToList();

                    dgHoadon.ItemsSource = listHoadon;

                    double totalRevenue = listHoadon.Sum(hd => hd.Tongtien ?? 0);
                    lblTongDoanhSo.Text = $"{totalRevenue:N0} VND";
                }
            }
            catch (Exception)
            {
                dgHoadon.ItemsSource = null;
                lblTongDoanhSo.Text = "0 VND";
            }
        }


        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var hitResult = VisualTreeHelper.HitTest(dgHoadon, e.GetPosition(dgHoadon));

            if (hitResult == null || !IsClickOnRow(e.OriginalSource as DependencyObject))
            {
                dgHoadon.SelectedItem = null; 
                Keyboard.ClearFocus(); 
            }
        }
        private bool IsClickOnRow(DependencyObject target)
        {
            while (target != null)
            {
                if (target is DataGridRow) return true;

                if (target is DataGrid) return false;

                target = VisualTreeHelper.GetParent(target);
            }
            return false;
        }


        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}