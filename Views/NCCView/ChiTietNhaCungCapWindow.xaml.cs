using PharmaDistributionApp.Models; // Đảm bảo đúng namespace của lớp Database hoặc Model của bạn
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
        // Thay đổi tham số truyền vào từ DataRowView thành Nhacungcap
        public ChiTietNhaCungCapWindow(Nhacungcap ncc)
        {
            InitializeComponent();

            // 1. Phím tắt ESC để thoát
            this.KeyDown += (s, e) => {
                if (e.Key == Key.Escape) this.Close();
            };

            // 2. Click chuột ra ngoài để hủy focus (MouseDown của Window)
            this.MouseDown += (s, e) => {
                // Chuyển focus về chính Window để DataGrid mất focus
                FocusManager.SetFocusedElement(this, this);
                if (dgSanPham != null) dgSanPham.UnselectAll();
            };

            if (ncc != null)
            {
                // Gán dữ liệu trực tiếp lên giao diện (Sửa lỗi thiếu hàm)
                lblMaNCC.Text = ncc.Mancc;
                lblTenNCC.Text = ncc.Tenncc?.ToUpper();
                lblSdt.Text = ncc.Sdt;
                lblEmail.Text = ncc.Email;
                lblDiaChi.Text = ncc.Diachi;

                // Tải danh sách sản phẩm
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
                    // SỬA TẠI ĐÂY: Sử dụng tên cột NHACUNGCAP thay cho MANCC
                    string sql = "SELECT MASP, TENSP, DVT, GIABAN FROM SANPHAM WHERE NHACUNGCAP = @mancc";

                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@mancc", maNCC);

                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        // Hiển thị lên DataGrid
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
        // Thêm hàm này vào trong class ChiTietNhaCungCapWindow
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Chỉ cho phép kéo khi nhấn chuột trái
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Kiểm tra xem chuột có click vào trong DataGrid (dgSanPham) hay không
            var hitResult = VisualTreeHelper.HitTest(dgSanPham, e.GetPosition(dgSanPham));

            // Nếu click ra ngoài bảng hoàn toàn (hitResult == null)
            // HOẶC click vào vùng trống trong bảng (không trúng dòng dữ liệu)
            if (hitResult == null || !IsClickOnRow(e.OriginalSource as DependencyObject))
            {
                dgSanPham.SelectedItem = null; // Bỏ chọn dòng
                Keyboard.ClearFocus(); // Bỏ focus bàn phím
            }
        }

        // Hàm phụ trợ: Kiểm tra xem đối tượng được click có phải là một phần của DataGridRow không
        private bool IsClickOnRow(DependencyObject target)
        {
            while (target != null)
            {
                // Nếu duyệt lên gặp DataGridRow -> Đang click trúng dòng -> Return true (Giữ selection)
                if (target is DataGridRow) return true;

                // Nếu duyệt lên gặp DataGrid mà chưa thấy Row -> Click vào vùng trắng -> Return false (Bỏ selection)
                if (target is DataGrid) return false;

                target = VisualTreeHelper.GetParent(target);
            }
            return false;
        }
    }
}