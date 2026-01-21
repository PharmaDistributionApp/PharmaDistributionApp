using PharmaDistributionApp.Models; 
using PharmaDistributionApp.Services;
using System;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PharmaDistributionApp.Views.NCCView
{
    public partial class ChiTietNhaCungCapWindow : Window
    {
        public ChiTietNhaCungCapWindow(Nhacungcap ncc)
        {
            InitializeComponent();
            this.KeyDown += (s, e) => {
                if (e.Key == Key.Escape) this.Close();
            };
            this.MouseDown += (s, e) => {
                FocusManager.SetFocusedElement(this, this);
                if (dgSanPham != null) dgSanPham.UnselectAll();
            };

            if (ncc != null)
            {
                lblMaNCC.Text = ncc.Mancc;
                lblTenNCC.Text = ncc.Tenncc?.ToUpper();
                lblSdt.Text = ncc.Sdt;
                lblEmail.Text = ncc.Email;
                lblDiaChi.Text = ncc.Diachi;
                LoadProductList(ncc.Mancc);
            }
        }
        private void LoadProductList(string maNCC)
        {
            try
            {
                using (var conn = new SQLiteConnection(Database.ConnectionString))
                {
                    conn.Open();
                    string sql = "SELECT MASP, TENSP, DVT, GIABAN FROM SANPHAM WHERE NHACUNGCAP = @mancc";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@mancc", maNCC);

                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dgSanPham.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách sản phẩm: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var hitResult = VisualTreeHelper.HitTest(dgSanPham, e.GetPosition(dgSanPham));

            if (hitResult == null || !IsClickOnRow(e.OriginalSource as DependencyObject))
            {
                dgSanPham.SelectedItem = null;
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
    }
}