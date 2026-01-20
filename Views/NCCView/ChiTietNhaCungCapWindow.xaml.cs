using PharmaDistributionApp.Models; // Đảm bảo đúng namespace của lớp Database hoặc Model của bạn
using PharmaDistributionApp.Services;
using System;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Input;

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

        private void LoadData(DataRowView row)
        {
            try
            {
                // 1. Hiển thị thông tin cơ bản của Nhà cung cấp từ dòng dữ liệu truyền vào
                string maNCC = row["MANCC"].ToString();
                lblMaNCC.Text = maNCC;
                lblTenNCC.Text = row["TENNCC"].ToString();
                lblSdt.Text = row["SDT"].ToString();
                lblEmail.Text = row["EMAIL"].ToString();
                lblDiaChi.Text = row["DIACHI"].ToString();

                // 2. Truy vấn danh sách sản phẩm thuộc nhà cung cấp này
                LoadProductList(maNCC);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hiển thị dữ liệu: " + ex.Message, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}