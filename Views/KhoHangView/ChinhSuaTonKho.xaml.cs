using Microsoft.EntityFrameworkCore;
using PharmaDistributionApp.Models;
using System;
using System.Linq;
using System.Windows;

namespace PharmaDistributionApp.Views.KhoHangView
{
    public partial class ChinhSuaTonKho : Window
    {
        private string _masp;
        private string _malo;
        private string _makho;

        public ChinhSuaTonKho(string masp, string malo, string makho)
        {
            InitializeComponent();
            _masp = masp;
            _malo = malo;
            _makho = makho;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var tonKho = context.Tonkhos.FirstOrDefault(t => t.Masp == _masp && t.Malo == _malo && t.Makho == _makho);

                    var sp = context.Sanphams.FirstOrDefault(s => s.Masp == _masp);
                    var kho = context.Khos.FirstOrDefault(k => k.Makho == _makho);

                    if (tonKho != null)
                    {
                        txtTenSP.Text = sp != null ? $"{sp.Tensp} ({_masp})" : _masp;
                        txtMaLovanKho.Text = $"Lô: {_malo}  |  Kho: {(kho != null ? kho.Tenkho : _makho)}";
                        txtSoLuong.Text = tonKho.Soluongton.ToString();

                        txtSoLuong.Focus();
                        txtSoLuong.SelectAll();
                    }
                    else
                    {
                        MessageBox.Show("Dữ liệu không tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                    }
                    var loHang = context.Lohangs.FirstOrDefault(l => l.Malo == _malo);
                    if (loHang != null && loHang.Hsd.HasValue)
                    {
                        dpHSD.SelectedDate = loHang.Hsd.Value.ToDateTime(TimeOnly.MinValue);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtSoLuong.Text, out int soLuongMoi) || soLuongMoi < 0)
            {
                MessageBox.Show("Số lượng phải là số nguyên dương!", "Cảnh báo");
                return;
            }

            if (dpHSD.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn Hạn sử dụng!", "Cảnh báo");
                return;
            }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var tonKho = context.Tonkhos.FirstOrDefault(t => t.Masp == _masp && t.Malo == _malo && t.Makho == _makho);
                    if (tonKho != null)
                    {
                        tonKho.Soluongton = soLuongMoi;
                        context.Entry(tonKho).State = EntityState.Modified;
                    }
                    var loHang = context.Lohangs.FirstOrDefault(l => l.Malo == _malo);
                    if (loHang != null)
                    {
                        loHang.Hsd = DateOnly.FromDateTime(dpHSD.SelectedDate.Value);
                        context.Entry(loHang).State = EntityState.Modified;
                    }
                    context.SaveChanges();

                    MessageBox.Show("Cập nhật thành công!", "Thông báo");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu: " + ex.Message);
            }
        }

        private void BtnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}