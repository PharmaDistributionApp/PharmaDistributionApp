using System.Linq;
using System.Windows;
using System.Windows.Media; // Bắt buộc có để dùng BrushConverter
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views.ProductView
{
    public partial class ChiTietSanPhamWindow : Window
    {
        private string _maSp;

        public ChiTietSanPhamWindow(string maSp)
        {
            InitializeComponent();
            _maSp = maSp;
            LoadChiTiet();
        }

        private void LoadChiTiet()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // 1. Tìm sản phẩm theo Mã
                    var sp = context.Sanphams.FirstOrDefault(x => x.Masp == _maSp);

                    if (sp != null)
                    {
                        // 2. Lấy tên loại (xử lý thủ công để an toàn)
                        string tenLoai = "Chưa phân loại";
                        if (!string.IsNullOrEmpty(sp.Maloai))
                        {
                            var loai = context.Loaisps.FirstOrDefault(x => x.Maloai == sp.Maloai);
                            if (loai != null) tenLoai = loai.Tenloai;
                        }

                        // 3. Tính toán Tổng số lượng tồn từ bảng TONKHO
                        var tongTon = context.Tonkhos
                                             .Where(t => t.Masp == _maSp)
                                             .Sum(t => (int?)t.Soluongton) ?? 0;

                        // 4. [LOGIC MỚI] Xử lý Trạng thái & Màu sắc
                        string textTrangThai;
                        string colorBack;
                        string colorFore;

                        // --- ƯU TIÊN 1: Kiểm tra GHI CHÚ ---
                        if (!string.IsNullOrEmpty(sp.Ghichu) && sp.Ghichu == "Đang nhập")
                        {
                            textTrangThai = "Đang nhập";
                            colorBack = "#E3F2FD"; // Xanh dương nhạt
                            colorFore = "#1565C0"; // Xanh dương đậm
                        }
                        // --- ƯU TIÊN 2: Nếu không có ghi chú, xét TỒN KHO ---
                        else
                        {
                            if (tongTon > 10)
                            {
                                textTrangThai = "Còn hàng";
                                colorBack = "#E8F5E9"; // Xanh lá nhạt
                                colorFore = "#2E7D32"; // Xanh lá đậm
                            }
                            else if (tongTon > 0 && tongTon <= 10)
                            {
                                textTrangThai = "Sắp hết hàng";
                                colorBack = "#FFF3E0"; // Cam nhạt
                                colorFore = "#EF6C00"; // Cam đậm
                            }
                            else
                            {
                                textTrangThai = "Hết hàng";
                                colorBack = "#FFEBEE"; // Đỏ nhạt
                                colorFore = "#C62828"; // Đỏ đậm
                            }
                        }

                        // 5. Gán dữ liệu lên giao diện
                        lblTenSP.Text = sp.Tensp;
                        txtMaSP.Text = sp.Masp;
                        txtLoai.Text = tenLoai;
                        txtDVT.Text = sp.Dvt;
                        txtNuocSX.Text = sp.Nuocsx ?? "Chưa cập nhật";
                        txtNhaCungCap.Text = sp.Nhacungcap ?? "Chưa cập nhật";
                        txtGiaBan.Text = string.Format("{0:N0} đ", sp.Giaban);

                        // Gán trạng thái và tô màu
                        txtTrangThai.Text = textTrangThai;

                        var converter = new BrushConverter();
                        borderTrangThai.Background = (Brush)converter.ConvertFrom(colorBack);
                        txtTrangThai.Foreground = (Brush)converter.ConvertFrom(colorFore);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi hiển thị chi tiết: " + ex.Message);
            }
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}