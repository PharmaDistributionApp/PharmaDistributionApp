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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PharmaDistributionApp.Views
{
    public partial class ChiTietHoaDonWindow : Window
    {
        private InvoiceViewModel _invoice;
        private int _vatRate = 0;
        private readonly string[] _approverRoles = { "Admin", "Quản lý", "Giám đốc", "Kế toán", "ADMIN", "QuanLy" };
        public ChiTietHoaDonWindow(InvoiceViewModel invoice)
        {
            InitializeComponent();
            _invoice = invoice;
            if (txtMaHD != null) txtMaHD.Text = _invoice.MaHD;
            LoadData();
            CheckApprovalMode();
        }

        // ===================================================================
        // PHẦN 1: TẢI DỮ LIỆU TỪ SQL (GIỮ NGUYÊN)
        // ===================================================================
        private void LoadData()
        {
            try
            {
                bool isExport = _invoice.LoaiHD == "Xuất" || _invoice.MaHD.StartsWith("HDX");
                string tblHead = isExport ? "HOADONXUAT" : "HOADONNHAP";
                string colID = isExport ? "SOHDXUAT" : "SOHDNHAP";
                string tblCT = isExport ? "CTHDXUAT" : "CTHDNHAP";
                string colGia = isExport ? "DONGIABAN" : "DONGIANHAP";
                string tblPartner = isExport ? "KHACHHANG" : "NHACUNGCAP";
                string colPartner = isExport ? "MAKH" : "MANCC";
                string colName = isExport ? "TENKH" : "TENNCC";

                using (var conn = new SQLiteConnection("Data Source=PharmaDB.db"))
                {
                    conn.Open();
                    // Load Header
                    string sqlH = $"SELECT H.*, P.{colName} as DoiTac, P.DIACHI FROM {tblHead} H LEFT JOIN {tblPartner} P ON H.{colPartner}=P.{colPartner} WHERE H.{colID}=@id";
                    var cmdH = new SQLiteCommand(sqlH, conn);
                    cmdH.Parameters.AddWithValue("@id", _invoice.MaHD);
                    var reader = cmdH.ExecuteReader();
                    if (reader.Read())
                    {
                        if (txtNgayLap != null) txtNgayLap.Text = DateTime.Parse(reader["NGAYLAP"].ToString()).ToString("dd/MM/yyyy HH:mm");
                        if (txtTenDoiTac != null) txtTenDoiTac.Text = reader["DoiTac"].ToString();
                        if (txtGhiChu != null) txtGhiChu.Text = reader["GHICHU"].ToString();
                        if (txtTrangThai != null) txtTrangThai.Text = reader["TRANGTHAI"].ToString();
                        if (txtNhanVien != null) txtNhanVien.Text = reader["MANV"].ToString();
                        if (txtDiaChi != null) txtDiaChi.Text = reader["DIACHI"].ToString();
                        _vatRate = Convert.ToInt32(reader["VAT"]);
                    }
                    reader.Close();

                    // Load Chi Tiet
                    string sqlD = $"SELECT CT.MASP, SP.TENSP, SP.DVT, CT.MALO, CT.SOLUONG, CT.{colGia} as Gia, CT.THANHTIEN FROM {tblCT} CT LEFT JOIN SANPHAM SP ON CT.MASP=SP.MASP WHERE CT.{colID}=@id";
                    var cmdD = new SQLiteCommand(sqlD, conn);
                    cmdD.Parameters.AddWithValue("@id", _invoice.MaHD);
                    var da = new SQLiteDataAdapter(cmdD);
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dgChiTiet != null)
                    {
                        var list = new List<ChiTietHoaDonItem>();
                        int i = 1;
                        decimal total = 0;
                        foreach (DataRow r in dt.Rows)
                        {
                            decimal tt = Convert.ToDecimal(r["THANHTIEN"]);
                            total += tt;
                            list.Add(new ChiTietHoaDonItem { STT = i++, MaSP = r["MASP"].ToString(), TenSP = r["TENSP"].ToString(), DonVi = r["DVT"].ToString(), MaLo = r["MALO"].ToString(), SoLuong = Convert.ToInt32(r["SOLUONG"]), DonGia = Convert.ToDecimal(r["Gia"]), ThanhTien = tt });
                        }
                        dgChiTiet.ItemsSource = list;
                        if (txtTienHang != null) txtTienHang.Text = $"{total:N0} VND";
                        decimal vat = total * _vatRate / 100;
                        if (txtVAT != null) txtVAT.Text = $"{vat:N0} VND";
                        if (txtTongTien != null) txtTongTien.Text = $"{total + vat:N0} VND";
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi load: " + ex.Message); }
        }

        // ===================================================================
        // PHẦN 2: LOGIC DUYỆT / TỪ CHỐI (QUAN TRỌNG)
        // ===================================================================
        private void CheckApprovalMode()
        {
            string role = UserSession.CurrentUser?.Chucvu?.Trim() ?? "";
            bool isBoss = _approverRoles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));

            // Chỉ hiện nút duyệt nếu: Sếp + Đơn đang chờ (PheDuyet=1)
            if (isBoss && _invoice.PheDuyet == 1)
            {
                if (pnlApprovalButtons != null) pnlApprovalButtons.Visibility = Visibility.Visible;
            }
            else
            {
                if (pnlApprovalButtons != null) pnlApprovalButtons.Visibility = Visibility.Collapsed;
            }
        }

        private void btnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("DUYỆT đơn này? (Kho sẽ được cập nhật)", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ProcessApproval(true);
            }
        }

        private void btnReject_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("TỪ CHỐI đơn này? (Đơn sẽ hủy)", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                ProcessApproval(false);
            }
        }

        private void ProcessApproval(bool isApproved)
        {
            // Chuẩn hóa ID
            string cleanID = _invoice.MaHD.Trim();

            // Xác định bảng dựa vào Mã (HDX... là Xuất, còn lại là Nhập)
            bool isExport = cleanID.StartsWith("HDX", StringComparison.OrdinalIgnoreCase);

            string tblH = isExport ? "HOADONXUAT" : "HOADONNHAP";
            string colID = isExport ? "SOHDXUAT" : "SOHDNHAP";

            using (var conn = new SQLiteConnection(Database.ConnectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        int rowsAffected = 0;

                        if (isApproved)
                        {
                            // A. DUYỆT ĐƠN
                            // 1. Cập nhật trạng thái Header
                            string sqlUpdate = $"UPDATE {tblH} SET PheDuyet=0, TRANGTHAI='Đã thanh toán' WHERE {colID}=@id";
                            var cmdUpdate = new SQLiteCommand(sqlUpdate, conn, trans);
                            cmdUpdate.Parameters.AddWithValue("@id", cleanID);
                            rowsAffected = cmdUpdate.ExecuteNonQuery();

                            // 2. [QUAN TRỌNG] GỌI HÀM CẬP NHẬT KHO NGAY TẠI ĐÂY
                            if (rowsAffected > 0)
                            {
                                // Truyền đúng connection và transaction đang mở vào
                                UpdateStockAfterApproval(conn, trans, isExport, cleanID);
                            }
                        }
                        else
                        {
                            // B. TỪ CHỐI (Chỉ cập nhật trạng thái, KHÔNG trừ kho)
                            string sqlReject = $"UPDATE {tblH} SET PheDuyet=0, TRANGTHAI='Đã hủy' WHERE {colID}=@id";
                            var cmdReject = new SQLiteCommand(sqlReject, conn, trans);
                            cmdReject.Parameters.AddWithValue("@id", cleanID);
                            rowsAffected = cmdReject.ExecuteNonQuery();
                        }

                        if (rowsAffected == 0)
                        {
                            trans.Rollback();
                            MessageBox.Show($"Lỗi: Không tìm thấy hóa đơn {cleanID} để cập nhật.", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        trans.Commit();

                        MessageBox.Show(isApproved ? "Duyệt và cập nhật kho thành công!" : "Đã từ chối đơn hàng.");
                        this.DialogResult = true; // Báo reload
                        Close();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show("Lỗi SQL: " + ex.Message);
                    }
                }
            }
        }
        private void UpdateStock(SQLiteConnection conn, SQLiteTransaction trans, bool isExport)
        {
            string tblCT = isExport ? "CTHDXUAT" : "CTHDNHAP";
            string colID = isExport ? "SOHDXUAT" : "SOHDNHAP";

            string sql = $"SELECT MASP, MALO, SOLUONG FROM {tblCT} WHERE {colID} = @id";
            using (var cmd = new SQLiteCommand(sql, conn, trans))
            {
                cmd.Parameters.AddWithValue("@id", _invoice.MaHD);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string msp = reader["MASP"].ToString();
                        string ml = reader["MALO"].ToString();
                        int sl = Convert.ToInt32(reader["SOLUONG"]);

                        if (isExport)
                        {
                            // Xuất duyệt -> TRỪ KHO
                            new SQLiteCommand($"UPDATE TONKHO SET SOLUONGTON = SOLUONGTON - {sl} WHERE MALO='{ml}'", conn, trans).ExecuteNonQuery();
                        }
                        else
                        {
                            // Nhập duyệt -> CỘNG KHO
                            // Insert nếu chưa có
                            string sqlIns = "INSERT OR IGNORE INTO TONKHO (MAKHO, MASP, MALO, SOLUONGTON) VALUES ('KHO01', @msp, @ml, 0)";
                            var cmdIns = new SQLiteCommand(sqlIns, conn, trans);
                            cmdIns.Parameters.AddWithValue("@msp", msp);
                            cmdIns.Parameters.AddWithValue("@ml", ml);
                            cmdIns.ExecuteNonQuery();

                            // Cộng
                            new SQLiteCommand($"UPDATE TONKHO SET SOLUONGTON = SOLUONGTON + {sl} WHERE MALO='{ml}'", conn, trans).ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        // Hàm cập nhật kho sau khi duyệt
        private void UpdateStockAfterApproval(SQLiteConnection conn, SQLiteTransaction trans, bool isExport, string invoiceID)
        {
            string tblCT = isExport ? "CTHDXUAT" : "CTHDNHAP";
            string colID = isExport ? "SOHDXUAT" : "SOHDNHAP";

            // Lấy chi tiết hàng hóa
            string sql = $"SELECT MASP, MALO, SOLUONG FROM {tblCT} WHERE {colID} = @id";

            using (var cmd = new SQLiteCommand(sql, conn, trans))
            {
                cmd.Parameters.AddWithValue("@id", invoiceID);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string ml = reader["MALO"].ToString();
                        string msp = reader["MASP"].ToString();
                        int sl = Convert.ToInt32(reader["SOLUONG"]);

                        if (!isExport)
                        {
                            // === NHẬP KHO (CỘNG) ===

                            // a. Tạo dòng tồn kho = 0 nếu chưa có (để tránh lỗi UPDATE không tìm thấy dòng)
                            // Giả sử mã kho mặc định là KHO_THUONG (hoặc bạn có thể lấy từ bảng PHIEUNHAP nếu có)
                            // Ở đây tôi để tạm KHO_THUONG, bạn có thể sửa logic chọn kho sau
                            string sqlIns = "INSERT OR IGNORE INTO TONKHO (MAKHO, MASP, MALO, SOLUONGTON) VALUES ('KHO_THUONG', @msp, @ml, 0)";
                            var cmdIns = new SQLiteCommand(sqlIns, conn, trans);
                            cmdIns.Parameters.AddWithValue("@msp", msp);
                            cmdIns.Parameters.AddWithValue("@ml", ml);
                            cmdIns.ExecuteNonQuery();

                            // b. Cộng số lượng
                            string sqlUp = "UPDATE TONKHO SET SOLUONGTON = SOLUONGTON + @sl WHERE MALO=@ml AND MASP=@msp";
                            var cmdUp = new SQLiteCommand(sqlUp, conn, trans);
                            cmdUp.Parameters.AddWithValue("@sl", sl);
                            cmdUp.Parameters.AddWithValue("@ml", ml);
                            cmdUp.Parameters.AddWithValue("@msp", msp);
                            cmdUp.ExecuteNonQuery();
                        }
                        else
                        {
                            // === XUẤT KHO (TRỪ) ===

                            string sqlUp = "UPDATE TONKHO SET SOLUONGTON = SOLUONGTON - @sl WHERE MALO=@ml AND MASP=@msp";
                            var cmdUp = new SQLiteCommand(sqlUp, conn, trans);
                            cmdUp.Parameters.AddWithValue("@sl", sl);
                            cmdUp.Parameters.AddWithValue("@ml", ml);
                            cmdUp.Parameters.AddWithValue("@msp", msp);
                            cmdUp.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        // ===================================================================
        // PHẦN 3: XUẤT PDF (GIỮ NGUYÊN)
        // ===================================================================
        private void btnExportPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("ddMMyy_HHmmss");
                SaveFileDialog dlg = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"HoaDon_{_invoice.MaHD}_{timestamp}.pdf" };

                if (dlg.ShowDialog() == true)
                {
                    Document doc = new Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter.GetInstance(doc, new FileStream(dlg.FileName, FileMode.Create));
                    doc.Open();

                    // --- SỬA LỖI Ở ĐÂY: Dùng System.IO.Path ---
                    string fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    if (!File.Exists(fontPath)) fontPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "tahoma.ttf");

                    BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    Font fTitle = new Font(bf, 18, Font.BOLD, BaseColor.BLUE);
                    Font fHeader = new Font(bf, 12, Font.BOLD, BaseColor.BLACK);
                    Font fNorm = new Font(bf, 11, Font.NORMAL, BaseColor.BLACK);
                    Font fRed = new Font(bf, 14, Font.BOLD, BaseColor.RED);

                    doc.Add(new Paragraph($"HÓA ĐƠN {_invoice.LoaiHD.ToUpper()}", fTitle) { Alignment = Element.ALIGN_CENTER });
                    doc.Add(new Paragraph(" ", fNorm));

                    // Info Table
                    PdfPTable tInfo = new PdfPTable(2); tInfo.WidthPercentage = 100;
                    PdfPCell c1 = new PdfPCell(); c1.Border = Rectangle.NO_BORDER;
                    c1.AddElement(new Paragraph($"Đối tác: {txtTenDoiTac.Text}", fHeader));
                    c1.AddElement(new Paragraph($"Địa chỉ: {txtDiaChi.Text}", fNorm));
                    tInfo.AddCell(c1);

                    PdfPCell c2 = new PdfPCell(); c2.Border = Rectangle.NO_BORDER; c2.HorizontalAlignment = Element.ALIGN_RIGHT;
                    c2.AddElement(new Paragraph($"Số HĐ: {_invoice.MaHD}", fHeader));
                    c2.AddElement(new Paragraph($"Ngày lập: {txtNgayLap.Text}", fNorm));
                    c2.AddElement(new Paragraph($"NV: {txtNhanVien.Text}", fNorm));
                    tInfo.AddCell(c2);
                    doc.Add(tInfo); doc.Add(new Paragraph(" ", fNorm));

                    // Detail Table
                    PdfPTable tDet = new PdfPTable(6);
                    tDet.WidthPercentage = 100;
                    tDet.SetWidths(new float[] { 10f, 15f, 35f, 10f, 15f, 15f });

                    string[] headers = { "STT", "Mã SP", "Tên SP", "SL", "Đơn Giá", "Thành Tiền" };
                    foreach (var h in headers)
                    {
                        PdfPCell c = new PdfPCell(new Phrase(h, fHeader));
                        c.HorizontalAlignment = Element.ALIGN_CENTER; c.BackgroundColor = BaseColor.LIGHT_GRAY; c.Padding = 5;
                        tDet.AddCell(c);
                    }

                    var items = dgChiTiet.ItemsSource as IEnumerable<dynamic>;
                    if (items != null)
                    {
                        foreach (var i in items)
                        {
                            var item = (ChiTietHoaDonItem)i;
                            AddCell(tDet, item.STT.ToString(), fNorm, Element.ALIGN_CENTER);
                            AddCell(tDet, item.MaSP, fNorm, Element.ALIGN_CENTER);
                            AddCell(tDet, item.TenSP, fNorm, Element.ALIGN_LEFT);
                            AddCell(tDet, item.SoLuong.ToString(), fNorm, Element.ALIGN_CENTER);
                            AddCell(tDet, item.DonGia.ToString("#,##0"), fNorm, Element.ALIGN_RIGHT);
                            AddCell(tDet, item.ThanhTien.ToString("#,##0"), fNorm, Element.ALIGN_RIGHT);
                        }
                    }
                    doc.Add(tDet); doc.Add(new Paragraph(" ", fNorm));

                    Paragraph pTotal = new Paragraph($"Tổng cộng: {txtTongTien.Text}", fRed);
                    pTotal.Alignment = Element.ALIGN_RIGHT;
                    doc.Add(pTotal);

                    doc.Close();

                    if (MessageBox.Show("Xuất PDF thành công! Mở file ngay?", "Thành công", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dlg.FileName, UseShellExecute = true });
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi xuất PDF: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void AddCell(PdfPTable t, string txt, Font f, int align)
        {
            PdfPCell c = new PdfPCell(new Phrase(txt, f));
            c.HorizontalAlignment = align; c.Padding = 5;
            t.AddCell(c);
        }

        // ===================================================================
        // PHẦN 4: UI EVENTS
        // ===================================================================
        private void Window_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) Close(); }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); }
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        private void btnClose_Click(object sender, RoutedEventArgs e) { Close(); }
    }

    // Class hỗ trợ hiển thị
    public class ChiTietHoaDonItem
    {
        public int STT { get; set; }
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string DonVi { get; set; }
        public string MaLo { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }
}