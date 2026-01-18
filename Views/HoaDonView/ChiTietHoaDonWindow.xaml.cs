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
    public partial class ChiTietHoaDonWindow : Window
    {
        private InvoiceViewModel _invoice;
        private int _vatRate = 0;
        private readonly string[] _approverRoles = { "Admin", "Quản lý", "Giám đốc", "Kế toán", "ADMIN", "admin" };

        public ChiTietHoaDonWindow(InvoiceViewModel invoice)
        {
            InitializeComponent();

            // [FIX LỖI ENCODING 1252]: Đăng ký bộ mã ký tự cho .NET Core/5+
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            _invoice = invoice;
            this.Loaded += (s, e) => {
                LoadHeaderInfo();
                LoadProductDetails_SQL();
                CheckApprovalMode();
            };
        }

        // --- 1. TẢI DỮ LIỆU (Đã giữ Fix Trim() để tránh lỗi không hiện data) ---
        private void LoadProductDetails_SQL()
        {
            if (_invoice == null || string.IsNullOrEmpty(_invoice.MaHD)) return;
            try
            {
                string cleanID = _invoice.MaHD.Trim();
                string sql = "";

                if (_invoice.LoaiHD == "Xuất")
                {
                    sql = $@"SELECT CT.MASP, IFNULL(SP.TENSP, 'SP xóa (' || CT.MASP || ')') AS TENSP, IFNULL(SP.DVT, '-') AS DVT, CT.MALO, CT.SOLUONG, CT.DONGIABAN AS DONGIA
                             FROM CTHDXUAT CT LEFT JOIN SANPHAM SP ON CT.MASP = SP.MASP WHERE CT.SOHDXUAT = '{cleanID}'";
                }
                else
                {
                    sql = $@"SELECT CT.MASP, IFNULL(SP.TENSP, 'SP xóa (' || CT.MASP || ')') AS TENSP, IFNULL(SP.DVT, '-') AS DVT, MAX(CT.MALO) AS MALO, SUM(CT.SOLUONG) AS SOLUONG, CT.DONGIANHAP AS DONGIA
                             FROM CTHDNHAP CT LEFT JOIN SANPHAM SP ON CT.MASP = SP.MASP WHERE CT.SOHDNHAP = '{cleanID}' GROUP BY CT.MASP, SP.TENSP, SP.DVT, CT.DONGIANHAP";
                }

                DataTable dt = Database.GetTable(sql);
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

                decimal tongTienHang = listItems.Sum(x => x.ThanhTien);
                decimal tienThue = tongTienHang * _vatRate / 100m;
                decimal tongCong = tongTienHang + tienThue;

                txtTienHang.Text = string.Format("{0:N0} VND", tongTienHang);
                txtVAT.Text = string.Format("{0:N0} VND", tienThue);
                txtTongTien.Text = string.Format("{0:N0} VND", tongCong);
            }
            catch (Exception ex) { MessageBox.Show("Lỗi load chi tiết: " + ex.Message); }
        }

        private void LoadHeaderInfo()
        {
            if (_invoice == null) return;
            txtMaHD.Text = _invoice.MaHD;
            txtNgayLap.Text = _invoice.NgayLap.ToString("dd/MM/yyyy");
            txtTenDoiTac.Text = _invoice.DoiTac;
            txtTrangThai.Text = _invoice.TrangThai;

            if (_invoice.TrangThai == "Đã thanh toán") { bdTrangThai.Background = Brushes.LightGreen; txtTrangThai.Foreground = Brushes.DarkGreen; }
            else if (_invoice.TrangThai == "Đã hủy") { bdTrangThai.Background = Brushes.Pink; txtTrangThai.Foreground = Brushes.DarkRed; }
            else if (_invoice.TrangThai == "Chờ duyệt") { bdTrangThai.Background = Brushes.LightYellow; txtTrangThai.Foreground = Brushes.Orange; }
            else if (_invoice.TrangThai == "Yêu cầu xóa") { bdTrangThai.Background = Brushes.Red; txtTrangThai.Foreground = Brushes.White; }
            else { bdTrangThai.Background = Brushes.Wheat; txtTrangThai.Foreground = Brushes.DarkOrange; }

            try
            {
                string cleanID = _invoice.MaHD.Trim();
                string table = _invoice.LoaiHD == "Xuất" ? "HOADONXUAT" : "HOADONNHAP";
                string colID = _invoice.LoaiHD == "Xuất" ? "SOHDXUAT" : "SOHDNHAP";
                string colDoiTac = _invoice.LoaiHD == "Xuất" ? "MAKH" : "MANCC";
                string tableDoiTac = _invoice.LoaiHD == "Xuất" ? "KHACHHANG" : "NHACUNGCAP";
                string colMaDT = _invoice.LoaiHD == "Xuất" ? "MAKH" : "MANCC";

                string sql = $@"SELECT H.VAT, H.GHICHU, D.DIACHI, D.SDT FROM {table} H LEFT JOIN {tableDoiTac} D ON H.{colDoiTac} = D.{colMaDT} WHERE H.{colID} = '{cleanID}'";

                DataTable dt = Database.GetTable(sql);
                if (dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    _vatRate = r["VAT"] != DBNull.Value ? Convert.ToInt32(r["VAT"]) : 0;
                    txtGhiChu.Text = r["GHICHU"].ToString();
                    if (string.IsNullOrEmpty(txtGhiChu.Text)) txtGhiChu.Text = "(Không có)";
                    txtDiaChi.Text = r["DIACHI"].ToString();
                    txtSDT.Text = r["SDT"].ToString();
                    lblVAT.Text = $"VAT ({_vatRate}%):";
                }
            }
            catch { txtDiaChi.Text = "---"; }
        }

        // --- 2. XUẤT PDF (Đã fix lỗi File đang mở & Lỗi Encoding) ---
        private void btnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Tạo tên file mặc định có ngày giờ để tránh trùng
                string timestamp = DateTime.Now.ToString("ddMMyy_HHmmss");
                SaveFileDialog dlg = new SaveFileDialog
                {
                    Filter = "PDF (*.pdf)|*.pdf",
                    FileName = $"HoaDon_{_invoice.MaHD}_{timestamp}.pdf"
                };

                if (dlg.ShowDialog() == true)
                {
                    // Kiểm tra xem file có đang bị khóa không TRƯỚC khi tạo PDF
                    if (IsFileLocked(new FileInfo(dlg.FileName)))
                    {
                        MessageBox.Show("File này đang được mở bởi chương trình khác.\nVui lòng đóng file PDF lại trước khi xuất!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Bắt đầu tạo PDF
                    Document doc = new Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter.GetInstance(doc, new FileStream(dlg.FileName, FileMode.Create));
                    doc.Open();

                    // Font Arial (Hỗ trợ tiếng Việt)
                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    Font fTitle = new Font(bf, 18, Font.BOLD, BaseColor.BLUE);
                    Font fHeader = new Font(bf, 12, Font.BOLD, BaseColor.BLACK);
                    Font fNorm = new Font(bf, 11, Font.NORMAL, BaseColor.BLACK);
                    Font fRed = new Font(bf, 14, Font.BOLD, BaseColor.RED);

                    doc.Add(new Paragraph($"HÓA ĐƠN {_invoice.LoaiHD.ToUpper()}", fTitle) { Alignment = Element.ALIGN_CENTER });
                    doc.Add(new Paragraph(" ", fNorm));

                    // Info
                    PdfPTable tInfo = new PdfPTable(2); tInfo.WidthPercentage = 100;
                    PdfPCell c1 = new PdfPCell(); c1.Border = Rectangle.NO_BORDER;
                    c1.AddElement(new Paragraph($"Đối tác: {_invoice.DoiTac}", fHeader));
                    c1.AddElement(new Paragraph($"Địa chỉ: {txtDiaChi.Text}", fNorm));
                    c1.AddElement(new Paragraph($"SĐT: {txtSDT.Text}", fNorm));
                    tInfo.AddCell(c1);

                    PdfPCell c2 = new PdfPCell(); c2.Border = Rectangle.NO_BORDER; c2.HorizontalAlignment = Element.ALIGN_RIGHT;
                    c2.AddElement(new Paragraph($"Số HĐ: {_invoice.MaHD}", fHeader));
                    c2.AddElement(new Paragraph($"Ngày lập: {_invoice.NgayLap:dd/MM/yyyy}", fNorm));
                    tInfo.AddCell(c2);
                    doc.Add(tInfo); doc.Add(new Paragraph(" ", fNorm));

                    // Table
                    PdfPTable tDet = new PdfPTable(6); tDet.WidthPercentage = 100;
                    tDet.SetWidths(new float[] { 10f, 15f, 35f, 10f, 15f, 15f });
                    string[] headers = { "STT", "Mã SP", "Tên SP", "SL", "Đơn Giá", "Thành Tiền" };
                    foreach (var h in headers)
                    {
                        PdfPCell c = new PdfPCell(new Phrase(h, fHeader));
                        c.HorizontalAlignment = Element.ALIGN_CENTER; c.BackgroundColor = BaseColor.LIGHT_GRAY; c.Padding = 5;
                        tDet.AddCell(c);
                    }
                    var items = dgChiTiet.ItemsSource as List<ChiTietHoaDonItem>;
                    if (items != null)
                    {
                        foreach (var i in items)
                        {
                            AddCell(tDet, i.STT.ToString(), fNorm, Element.ALIGN_CENTER);
                            AddCell(tDet, i.MaSP, fNorm, Element.ALIGN_CENTER);
                            AddCell(tDet, i.TenSP, fNorm, Element.ALIGN_LEFT);
                            AddCell(tDet, i.SoLuong.ToString(), fNorm, Element.ALIGN_CENTER);
                            AddCell(tDet, i.DonGia.ToString("#,##0"), fNorm, Element.ALIGN_RIGHT);
                            AddCell(tDet, i.ThanhTien.ToString("#,##0"), fNorm, Element.ALIGN_RIGHT);
                        }
                    }
                    doc.Add(tDet); doc.Add(new Paragraph(" ", fNorm));

                    // Total
                    Paragraph pTotal = new Paragraph($"Tổng cộng: {txtTongTien.Text}", fRed);
                    pTotal.Alignment = Element.ALIGN_RIGHT;
                    doc.Add(pTotal);

                    doc.Close();

                    if (MessageBox.Show("Xuất PDF thành công! Mở file ngay?", "Thành công", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dlg.FileName, UseShellExecute = true });
                    }
                }
            }
            catch (IOException)
            {
                MessageBox.Show("File đang được mở bởi chương trình khác. Vui lòng đóng lại trước!", "Lỗi file", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi xuất PDF: " + ex.Message);
            }
        }

        private void AddCell(PdfPTable t, string txt, Font f, int align)
        {
            PdfPCell c = new PdfPCell(new Phrase(txt, f)); c.HorizontalAlignment = align; c.Padding = 5; t.AddCell(c);
        }

        // Hàm kiểm tra file có bị khóa không
        private bool IsFileLocked(FileInfo file)
        {
            if (!file.Exists) return false;
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None)) { stream.Close(); }
            }
            catch (IOException) { return true; }
            return false;
        }

        // --- 3. DUYỆT (Giữ nguyên) ---
        private void CheckApprovalMode()
        {
            if (UserSession.CurrentUser == null) return;
            string role = UserSession.CurrentUser.Chucvu?.Trim();
            bool isBoss = _approverRoles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
            if (_invoice.TrangThai == "Chờ duyệt" && isBoss) { pnlApprovalButtons.Visibility = Visibility.Visible; btnApprove.Content = "DUYỆT ĐƠN"; btnReject.Content = "TỪ CHỐI"; return; }
            if (_invoice.TrangThai == "Yêu cầu xóa" && isBoss) { pnlApprovalButtons.Visibility = Visibility.Visible; btnApprove.Content = "ĐỒNG Ý XÓA"; btnReject.Content = "GIỮ LẠI"; bdTrangThai.Background = Brushes.Red; txtTrangThai.Foreground = Brushes.White; return; }
            pnlApprovalButtons.Visibility = Visibility.Collapsed;
        }
        private void btnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_invoice.TrangThai == "Yêu cầu xóa") { if (MessageBox.Show("ĐỒNG Ý XÓA?", "Duyệt", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { UpdateStatusInDB("Đã hủy"); Close(); } return; }
            if (MessageBox.Show("DUYỆT hóa đơn này?", "Duyệt", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { UpdateStatusInDB("Đã thanh toán"); Close(); }
        }
        private void btnReject_Click(object sender, RoutedEventArgs e)
        {
            if (_invoice.TrangThai == "Yêu cầu xóa") { if (MessageBox.Show("HỦY yêu cầu xóa?", "Duyệt", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { UpdateStatusInDB("Đã thanh toán"); Close(); } return; }
            if (MessageBox.Show("Từ chối sẽ HỦY hóa đơn?", "Duyệt", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { UpdateStatusInDB("Đã hủy"); Close(); }
        }
        private void UpdateStatusInDB(string s)
        {
            string t = _invoice.LoaiHD == "Xuất" ? "HOADONXUAT" : "HOADONNHAP"; string id = _invoice.LoaiHD == "Xuất" ? "SOHDXUAT" : "SOHDNHAP";
            using (var c = new SQLiteConnection("Data Source=PharmaDB.db")) { c.Open(); new SQLiteCommand($"UPDATE {t} SET TRANGTHAI = '{s}' WHERE {id} = '{_invoice.MaHD}'", c).ExecuteNonQuery(); }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Close(); }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Cho phép kéo cửa sổ khi giữ chuột trái
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }

            // Nếu vị trí click KHÔNG nằm trong DataGrid -> Bỏ chọn dòng (Unselect)
            // HitTest giúp kiểm tra xem chuột có đang nằm trên DataGrid hay không
            IInputElement element = InputHitTest(e.GetPosition(this));

            // Nếu element click vào không thuộc cây visual của DataGrid thì bỏ chọn
            if (element != null)
            {
                DependencyObject depObj = element as DependencyObject;
                bool isInsideDataGrid = false;

                while (depObj != null)
                {
                    if (depObj == dgChiTiet)
                    {
                        isInsideDataGrid = true;
                        break;
                    }
                    depObj = VisualTreeHelper.GetParent(depObj);
                }

                if (!isInsideDataGrid)
                {
                    dgChiTiet.UnselectAll();
                }
            }
        }
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}