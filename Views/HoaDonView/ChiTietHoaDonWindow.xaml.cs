using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.Win32;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PharmaDistributionApp.Views
{
    // Class chứa dữ liệu cho từng dòng trong bảng chi tiết
    public class ChiTietHoaDonItem
    {
        public int STT { get; set; }
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string DonVi { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string MaLo { get; set; }

        public decimal ThanhTien => SoLuong * DonGia;
        // Format tiền tệ Việt Nam
        public string DonGiaStr => string.Format("{0:N0} VND", DonGia);
        public string ThanhTienStr => string.Format("{0:N0} VND", ThanhTien);
    }

    public partial class ChiTietHoaDonWindow : Window
    {
        private InvoiceViewModel _invoice;
        private int _vatRate = 0;
        private readonly string[] _approverRoles = { "Admin", "Quản lý", "Giám đốc", "Kế toán", "ADMIN", "admin" };

        public ChiTietHoaDonWindow(InvoiceViewModel invoice)
        {
            InitializeComponent();
            _invoice = invoice;

            // Sự kiện Loaded: Chạy khi cửa sổ hiện lên
            this.Loaded += (s, e) => {
                try
                {
                    LoadHeaderInfo();        // 1. Tải thông tin chung (Header)
                    LoadProductDetails_SQL(); // 2. Tải danh sách sản phẩm (Grid)
                    CheckApprovalMode();      // 3. Kiểm tra quyền duyệt
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khởi tạo cửa sổ: " + ex.Message);
                }
            };
        }

        // ===================================================================
        // PHẦN 1: TẢI DỮ LIỆU TỪ SQL (ĐÃ SỬA LỖI TRUY VẤN)
        // ===================================================================
        private void LoadProductDetails_SQL()
        {
            if (_invoice == null || string.IsNullOrEmpty(_invoice.MaHD)) return;

            try
            {
                // [FIX LỖI 1]: Cắt khoảng trắng thừa để ID khớp chính xác
                string cleanID = _invoice.MaHD.Trim();
                string sql = "";

                if (_invoice.LoaiHD == "Xuất")
                {
                    // Lưu ý: Cột giá bán thường là DONGIABAN
                    sql = $@"
                        SELECT CT.MASP, 
                               IFNULL(SP.TENSP, 'SP không tồn tại (' || CT.MASP || ')') AS TENSP, 
                               IFNULL(SP.DVT, '-') AS DVT, 
                               CT.MALO, 
                               CT.SOLUONG, 
                               CT.DONGIABAN AS DONGIA
                        FROM CTHDXUAT CT
                        LEFT JOIN SANPHAM SP ON CT.MASP = SP.MASP
                        WHERE CT.SOHDXUAT = '{cleanID}'";
                }
                else
                {
                    // Lưu ý: Cột giá nhập thường là DONGIANHAP
                    // Dùng GROUP BY để gộp nếu có nhiều dòng trùng mã (tùy logic)
                    sql = $@"
                        SELECT CT.MASP, 
                               IFNULL(SP.TENSP, 'SP không tồn tại (' || CT.MASP || ')') AS TENSP, 
                               IFNULL(SP.DVT, '-') AS DVT, 
                               MAX(CT.MALO) AS MALO, 
                               SUM(CT.SOLUONG) AS SOLUONG, 
                               CT.DONGIANHAP AS DONGIA
                        FROM CTHDNHAP CT
                        LEFT JOIN SANPHAM SP ON CT.MASP = SP.MASP
                        WHERE CT.SOHDNHAP = '{cleanID}'
                        GROUP BY CT.MASP, SP.TENSP, SP.DVT, CT.DONGIANHAP";
                }

                DataTable dt = Database.GetTable(sql);

                // [FIX LỖI 2]: Nếu không có dữ liệu, kiểm tra lại ID
                if (dt.Rows.Count == 0)
                {
                    // (Tùy chọn) Uncomment dòng dưới để debug nếu danh sách vẫn trống
                    // MessageBox.Show($"Không tìm thấy chi tiết cho HĐ: {cleanID}\nSQL: {sql}");
                }

                var listItems = new List<ChiTietHoaDonItem>();
                int stt = 1;

                foreach (DataRow row in dt.Rows)
                {
                    listItems.Add(new ChiTietHoaDonItem
                    {
                        STT = stt++,
                        MaSP = row["MASP"].ToString(),
                        TenSP = row["TENSP"].ToString(),
                        DonVi = row["DVT"].ToString(),
                        MaLo = row["MALO"].ToString(),
                        SoLuong = Convert.ToInt32(row["SOLUONG"]),
                        DonGia = Convert.ToDecimal(row["DONGIA"])
                    });
                }

                dgChiTiet.ItemsSource = listItems;

                // Tính toán tổng tiền
                decimal tongTienHang = listItems.Sum(x => x.ThanhTien);
                decimal tienThue = tongTienHang * _vatRate / 100m;
                decimal tongCong = tongTienHang + tienThue;

                txtTienHang.Text = string.Format("{0:N0} VND", tongTienHang);
                txtVAT.Text = string.Format("{0:N0} VND", tienThue);
                txtTongTien.Text = string.Format("{0:N0} VND", tongCong);
            }
            catch (Exception ex)
            {
                // [QUAN TRỌNG]: Hiện lỗi để biết sai tên cột nào
                MessageBox.Show("Lỗi tải chi tiết hóa đơn: " + ex.Message, "Lỗi SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHeaderInfo()
        {
            if (_invoice == null) return;

            // Hiển thị thông tin cơ bản ngay lập tức (không cần SQL)
            txtMaHD.Text = _invoice.MaHD;
            txtNgayLap.Text = _invoice.NgayLap.ToString("dd/MM/yyyy");
            txtTenDoiTac.Text = _invoice.DoiTac;
            txtTrangThai.Text = _invoice.TrangThai;

            // Tô màu trạng thái
            if (_invoice.TrangThai == "Đã thanh toán") { bdTrangThai.Background = Brushes.LightGreen; txtTrangThai.Foreground = Brushes.DarkGreen; }
            else if (_invoice.TrangThai == "Đã hủy") { bdTrangThai.Background = Brushes.Pink; txtTrangThai.Foreground = Brushes.DarkRed; }
            else if (_invoice.TrangThai == "Chờ duyệt") { bdTrangThai.Background = Brushes.LightYellow; txtTrangThai.Foreground = Brushes.Orange; }
            else if (_invoice.TrangThai == "Yêu cầu xóa") { bdTrangThai.Background = Brushes.Red; txtTrangThai.Foreground = Brushes.White; }
            else { bdTrangThai.Background = Brushes.Wheat; txtTrangThai.Foreground = Brushes.DarkOrange; }

            // Truy vấn thêm thông tin phụ (VAT, Ghi chú, Địa chỉ đối tác)
            try
            {
                string cleanID = _invoice.MaHD.Trim();
                string table = _invoice.LoaiHD == "Xuất" ? "HOADONXUAT" : "HOADONNHAP";
                string colID = _invoice.LoaiHD == "Xuất" ? "SOHDXUAT" : "SOHDNHAP";

                string colDoiTac = _invoice.LoaiHD == "Xuất" ? "MAKH" : "MANCC";
                string tableDoiTac = _invoice.LoaiHD == "Xuất" ? "KHACHHANG" : "NHACUNGCAP";
                string colMaDT = _invoice.LoaiHD == "Xuất" ? "MAKH" : "MANCC";

                string sql = $@"
                    SELECT H.VAT, H.GHICHU, D.DIACHI, D.SDT 
                    FROM {table} H 
                    LEFT JOIN {tableDoiTac} D ON H.{colDoiTac} = D.{colMaDT} 
                    WHERE H.{colID} = '{cleanID}'";

                DataTable dt = Database.GetTable(sql);
                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    _vatRate = r["VAT"] != DBNull.Value ? Convert.ToInt32(r["VAT"]) : 0;

                    txtGhiChu.Text = r["GHICHU"].ToString();
                    if (string.IsNullOrWhiteSpace(txtGhiChu.Text)) txtGhiChu.Text = "(Không có)";

                    txtDiaChi.Text = r["DIACHI"].ToString();
                    txtSDT.Text = r["SDT"].ToString();
                    lblVAT.Text = $"VAT ({_vatRate}%):";
                }
            }
            catch (Exception ex)
            {
                // Nếu lỗi truy vấn thông tin phụ, set mặc định và báo lỗi nhẹ
                txtDiaChi.Text = "---";
                // MessageBox.Show("Lỗi tải thông tin phụ: " + ex.Message); // Có thể ẩn đi nếu không quan trọng
            }
        }

        // ===================================================================
        // PHẦN 2: LOGIC DUYỆT (GIỮ NGUYÊN)
        // ===================================================================
        private void CheckApprovalMode()
        {
            if (UserSession.CurrentUser == null) return;
            string role = UserSession.CurrentUser.Chucvu?.Trim();
            bool isBoss = _approverRoles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

            if (_invoice.TrangThai == "Chờ duyệt" && isBoss)
            {
                pnlApprovalButtons.Visibility = Visibility.Visible;
                btnApprove.Content = "DUYỆT ĐƠN";
                btnReject.Content = "TỪ CHỐI";
                return;
            }
            if (_invoice.TrangThai == "Yêu cầu xóa" && isBoss)
            {
                pnlApprovalButtons.Visibility = Visibility.Visible;
                btnApprove.Content = "ĐỒNG Ý XÓA";
                btnReject.Content = "GIỮ LẠI";
                bdTrangThai.Background = Brushes.Red;
                txtTrangThai.Foreground = Brushes.White;
                return;
            }
            pnlApprovalButtons.Visibility = Visibility.Collapsed;
        }

        private void btnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_invoice.TrangThai == "Yêu cầu xóa")
            {
                if (MessageBox.Show("Xác nhận ĐỒNG Ý XÓA?", "Duyệt xóa", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    UpdateStatusInDB("Đã hủy");
                    Close();
                }
                return;
            }
            if (MessageBox.Show("Xác nhận DUYỆT hóa đơn này?", "Phê duyệt", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                UpdateStatusInDB("Đã thanh toán");
                Close();
            }
        }

        private void btnReject_Click(object sender, RoutedEventArgs e)
        {
            if (_invoice.TrangThai == "Yêu cầu xóa")
            {
                if (MessageBox.Show("HỦY yêu cầu xóa?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    UpdateStatusInDB("Đã thanh toán");
                    Close();
                }
                return;
            }
            if (MessageBox.Show("Từ chối sẽ HỦY hóa đơn này?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                UpdateStatusInDB("Đã hủy");
                Close();
            }
        }

        private void UpdateStatusInDB(string newStatus)
        {
            string table = _invoice.LoaiHD == "Xuất" ? "HOADONXUAT" : "HOADONNHAP";
            string colID = _invoice.LoaiHD == "Xuất" ? "SOHDXUAT" : "SOHDNHAP";
            using (var conn = new SQLiteConnection("Data Source=PharmaDB.db"))
            {
                conn.Open();
                new SQLiteCommand($"UPDATE {table} SET TRANGTHAI = '{newStatus}' WHERE {colID} = '{_invoice.MaHD}'", conn).ExecuteNonQuery();
            }
        }

        // ===================================================================
        // PHẦN 3: XUẤT PDF (GIỮ NGUYÊN)
        // ===================================================================
        private void btnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"HoaDon_{_invoice.MaHD}.pdf" };
                if (dlg.ShowDialog() == true)
                {
                    Document doc = new Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter.GetInstance(doc, new FileStream(dlg.FileName, FileMode.Create));
                    doc.Open();

                    // Font tiếng Việt
                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    Font fTitle = new Font(bf, 18, Font.BOLD, BaseColor.BLUE);
                    Font fNorm = new Font(bf, 11, Font.NORMAL, BaseColor.BLACK);

                    doc.Add(new Paragraph($"HÓA ĐƠN {_invoice.LoaiHD.ToUpper()}", fTitle) { Alignment = Element.ALIGN_CENTER });
                    doc.Add(new Paragraph($"\nĐối tác: {_invoice.DoiTac}\nNgày lập: {_invoice.NgayLap:dd/MM/yyyy}\nTổng tiền: {txtTongTien.Text}", fNorm));

                    doc.Close();
                    MessageBox.Show("Xuất PDF thành công!");
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        // Helper Events
        private void Window_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Close(); }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { try { DragMove(); } catch { } }
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}