using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;

namespace PharmaDistributionApp.Views
{
    // Class hỗ trợ lưu thông tin lô hàng
    public class LotInfo
    {
        public string MaLo { get; set; }
        public string HSD { get; set; }
        public int TonKho { get; set; }
    }

    public partial class AddInvoiceWindow : Window
    {
        private ObservableCollection<InvoiceTempItem> _tempItems = new ObservableCollection<InvoiceTempItem>();
        private List<LotInfo> _currentProductLots = new List<LotInfo>();
        private int _totalAvailableStock = 0;
        private string _currentMaNV = "";
        private Stack<int> _addHistory = new Stack<int>();
        private readonly string[] _highLevelRoles = { "Admin", "Quản lý", "Giám đốc", "Kế toán" };

        public AddInvoiceWindow()
        {
            InitializeComponent();

            if (UserSession.IsLoggedIn && UserSession.CurrentUser != null)
            {
                _currentMaNV = UserSession.CurrentUser.Manv;
                txtNhanVien.Text = $"{UserSession.CurrentUser.Tennv} ({_currentMaNV})";
            }
            else { _currentMaNV = "NV001"; txtNhanVien.Text = "Admin (NV001)"; }

            txtNgayLap.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            dgChiTiet.ItemsSource = _tempItems;
            LoadProducts();
            InvoiceType_Checked(null, null);
        }

        // --- HÀM LOGIC MỚI: TÌM MÃ LÔ TRỐNG (L001, L002...) ---
        private List<string> GetNextBatchCodes(int countNeeded)
        {
            var usedNumbers = new HashSet<int>();

            // 1. Lấy tất cả mã lô đang tồn tại trong Database
            try
            {
                DataTable dt = Database.GetTable("SELECT MALO FROM LOHANG WHERE MALO LIKE 'L%'");
                foreach (DataRow row in dt.Rows)
                {
                    string dbCode = row["MALO"].ToString();
                    // Cắt bỏ chữ 'L', lấy phần số
                    if (dbCode.Length > 1 && int.TryParse(dbCode.Substring(1), out int num))
                    {
                        usedNumbers.Add(num);
                    }
                }
            }
            catch { }

            // 2. Lấy các mã lô đang nằm trong danh sách tạm (chưa lưu vào DB)
            // Để tránh trường hợp bạn thêm 2 lần liên tiếp: lần 1 lấy L003, lần 2 cũng lấy L003
            foreach (var item in _tempItems)
            {
                if (!string.IsNullOrEmpty(item.MaLo) && item.MaLo.StartsWith("L") && item.MaLo.Length > 1)
                {
                    if (int.TryParse(item.MaLo.Substring(1), out int num))
                    {
                        usedNumbers.Add(num);
                    }
                }
            }

            // 3. Tìm các số còn trống bắt đầu từ 1
            var result = new List<string>();
            int currentCheck = 1;

            while (result.Count < countNeeded)
            {
                // Nếu số này chưa dùng -> Lấy nó
                if (!usedNumbers.Contains(currentCheck))
                {
                    result.Add($"L{currentCheck:D3}"); // Format L001, L002...

                    // Đánh dấu là đã dùng tạm thời để vòng lặp tiếp theo không lấy trùng
                    usedNumbers.Add(currentCheck);
                }
                currentCheck++;

                // Safety break (tránh lặp vô tận nếu logic sai)
                if (currentCheck > 999999) break;
            }

            return result;
        }

        // --- SỰ KIỆN TEXT CHANGED: HIỂN THỊ PREVIEW MÃ LÔ ---
        private void txtSoLoTach_TextChanged(object sender, TextChangedEventArgs e)
        {
            GenerateBatchCodesPreview();
        }

        private void GenerateBatchCodesPreview()
        {
            if (txtSoLoTach == null || txtMaLoList == null) return;

            if (int.TryParse(txtSoLoTach.Text, out int count) && count > 0)
            {
                // Gọi hàm logic mới để xem trước các mã sẽ được tạo
                List<string> codes = GetNextBatchCodes(count);
                txtMaLoList.Text = string.Join(", ", codes);
            }
            else
            {
                txtMaLoList.Text = "(Nhập số lượng lô)";
            }
        }

        // --- XỬ LÝ THÊM SẢN PHẨM (SỬA LOGIC TẠO MÃ) ---
        private void btnThemSP_Click(object sender, RoutedEventArgs e)
        {
            if (cboSanPham.SelectedItem == null) { MessageBox.Show("Vui lòng chọn sản phẩm!"); return; }

            int.TryParse(txtSoLuong.Text, out int slDat);
            string rawGia = txtDonGia.Text.Replace(" VND", "").Replace(",", "").Replace(".", "").Trim();
            decimal.TryParse(rawGia, out decimal donGia);

            if (slDat <= 0) { MessageBox.Show("Số lượng phải lớn hơn 0!"); return; }

            int tongSL = slDat;
            var rowSP = cboSanPham.SelectedItem as DataRowView;
            string maSP = rowSP["MASP"].ToString();
            string tenSP = rowSP["TenSP"].ToString();
            string dvt = rowSP["DVT"].ToString();

            // === 1. NHẬP HÀNG (Tạo lô mới L001...) ===
            if (rbNhap.IsChecked == true)
            {
                if (dpNSX.SelectedDate == null || dpHSD.SelectedDate == null) { MessageBox.Show("Chọn NSX và HSD!"); return; }
                if (dpHSD.SelectedDate <= dpNSX.SelectedDate) { MessageBox.Show("HSD phải sau NSX!"); return; }
                if (!int.TryParse(txtSoLoTach.Text, out int soLoTach) || soLoTach <= 0) soLoTach = 1;
                if (tongSL < soLoTach) { MessageBox.Show($"Tổng SL ({tongSL}) nhỏ hơn số lô ({soLoTach})!"); return; }

                _addHistory.Push(soLoTach);

                int slMoiLo = tongSL / soLoTach;
                int slDu = tongSL % soLoTach;

                // [SỬA ĐỔI] Lấy danh sách mã lô trống thực tế
                List<string> newCodes = GetNextBatchCodes(soLoTach);

                for (int i = 0; i < soLoTach; i++)
                {
                    int slThucTe = slMoiLo;
                    if (i == soLoTach - 1) slThucTe += slDu;

                    // Lấy mã từ danh sách đã tạo
                    string autoMaLo = newCodes[i];

                    _tempItems.Add(new InvoiceTempItem
                    {
                        MaSP = maSP,
                        TenSP = tenSP,
                        DonVi = dvt,
                        MaLo = autoMaLo,
                        NSX = dpNSX.SelectedDate.Value.ToString("yyyy-MM-dd"),
                        HSD = dpHSD.SelectedDate.Value.ToString("yyyy-MM-dd"),
                        SoLuong = slThucTe,
                        DonGia = donGia
                    });
                }
                CalculateTotal();
                txtSoLuong.Text = "0";
                txtSoLoTach.Text = "1";
                return;
            }

            // === 2. XUẤT HÀNG (Giữ nguyên logic cũ) ===
            if (tongSL > _totalAvailableStock) { MessageBox.Show($"Kho không đủ hàng! Còn {_totalAvailableStock}"); return; }

            int canLay = tongSL;
            int countAdded = 0;
            foreach (var lot in _currentProductLots)
            {
                if (canLay <= 0) break;
                if (lot.TonKho > 0)
                {
                    int take = Math.Min(canLay, lot.TonKho);
                    var exist = _tempItems.FirstOrDefault(x => x.MaSP == maSP && x.MaLo == lot.MaLo);
                    if (exist != null)
                    {
                        exist.SoLuong += take;
                        int i = _tempItems.IndexOf(exist); _tempItems.RemoveAt(i); _tempItems.Insert(i, exist);
                    }
                    else
                    {
                        _tempItems.Add(new InvoiceTempItem
                        {
                            MaSP = maSP,
                            TenSP = tenSP,
                            DonVi = dvt,
                            MaLo = lot.MaLo,
                            SoLuong = take,
                            DonGia = donGia
                        });
                        countAdded++;
                    }
                    canLay -= take;
                    lot.TonKho -= take;
                }
            }
            if (countAdded > 0) _addHistory.Push(countAdded);
            CalculateTotal();
            _totalAvailableStock -= tongSL;
            lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
            txtSoLuong.Text = "0";
        }

        // --- CÁC HÀM UI KHÁC GIỮ NGUYÊN ---
        private void InvoiceType_Checked(object sender, RoutedEventArgs e)
        {
            if (txtMaHD == null) return;
            _tempItems.Clear();
            _addHistory.Clear();
            CalculateTotal();
            txtSoLuong.Text = "0";
            lblTonKho.Text = "Tồn: 0";
            _currentProductLots.Clear();
            _totalAvailableStock = 0;

            bool isExport = rbXuat.IsChecked == true;
            if (isExport)
            {
                lblDoiTac.Text = "Khách hàng";
                GenerateInvoiceCode("HDX", "HOADONXUAT", "SOHDXUAT");
                LoadPartners("KHACHHANG", "MAKH", "TENKH");
                pnlNhapHang.Visibility = Visibility.Collapsed;
                lblTonKho.Visibility = Visibility.Visible;
            }
            else
            {
                lblDoiTac.Text = "Nhà cung cấp";
                GenerateInvoiceCode("HDN", "HOADONNHAP", "SOHDNHAP");
                LoadPartners("NHACUNGCAP", "MANCC", "TENNCC");
                txtDonGia.Text = "0 VND";
                pnlNhapHang.Visibility = Visibility.Visible;
                lblTonKho.Visibility = Visibility.Collapsed;
                dpNSX.SelectedDate = DateTime.Now;
                dpHSD.SelectedDate = DateTime.Now.AddYears(2);
                txtSoLoTach.Text = "1";
                GenerateBatchCodesPreview();
            }
            if (cboSanPham != null && cboSanPham.SelectedItem != null)
            {
                cboSanPham_SelectionChanged(cboSanPham, null);
            }
        }

        private void LoadProducts()
        {
            try
            {
                // [SỬA]: Chỉ lấy GIABAN vì đây là cột giá trị duy nhất bạn dùng
                string sql = "SELECT MASP, TENSP, DVT, GIABAN FROM SANPHAM";
                DataTable dt = Database.GetTable(sql);
                cboSanPham.ItemsSource = dt.DefaultView;
                cboSanPham.DisplayMemberPath = "TENSP";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải sản phẩm: " + ex.Message);
            }
        }

        private void LoadPartners(string table, string idCol, string nameCol)
        {
            var dt = Database.GetTable($"SELECT {idCol}, {nameCol} FROM {table}");
            var list = new List<dynamic>();
            foreach (DataRow r in dt.Rows) list.Add(new { Value = r[idCol], Display = $"{r[nameCol]} - [{r[idCol]}]" });
            cboDoiTac.ItemsSource = list;
        }

        private void GenerateInvoiceCode(string prefix, string table, string col)
        {
            try
            {
                var dt = Database.GetTable($"SELECT {col} FROM {table} WHERE {col} LIKE '{prefix}%' ORDER BY {col} DESC LIMIT 1");
                if (dt.Rows.Count > 0)
                {
                    string numPart = dt.Rows[0][0].ToString().Substring(prefix.Length);
                    txtMaHD.Text = int.TryParse(numPart, out int n) ? prefix + (n + 1).ToString("D3") : prefix + "001";
                }
                else txtMaHD.Text = prefix + "001";
            }
            catch { txtMaHD.Text = prefix + DateTime.Now.ToString("yyMMddHHmm"); }
        }

        private void cboSanPham_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSanPham.SelectedItem is DataRowView row)
            {
                string maSP = row["MASP"].ToString();

                // 1. Lấy GIÁ GỐC từ cột GIABAN (Theo yêu cầu của bạn)
                decimal giaGoc = 0;
                if (row.DataView.Table.Columns.Contains("GIABAN"))
                {
                    decimal.TryParse(row["GIABAN"].ToString(), out giaGoc);
                }

                // 2. Tính toán giá hiển thị
                decimal giaHienThi = giaGoc;

                if (rbXuat.IsChecked == true)
                {
                    // Xuất: Tăng 5% từ giá gốc
                    giaHienThi = giaGoc * 1.05m;
                }
                // Nhập: Giữ nguyên giá gốc (GIABAN)

                // 3. Hiển thị
                txtDonGia.Text = string.Format("{0:N0} VND", giaHienThi);

                // 4. Load tồn kho (Chỉ cần khi Xuất)
                if (rbXuat.IsChecked == true)
                {
                    string sqlLo = $@"SELECT L.MALO, L.HSD, IFNULL(T.SOLUONGTON, 0) AS SOLUONGTON 
                                      FROM LOHANG L LEFT JOIN TONKHO T ON L.MALO = T.MALO 
                                      WHERE L.MASP = '{maSP}' ORDER BY L.HSD ASC";

                    DataTable dtLo = Database.GetTable(sqlLo);
                    _currentProductLots.Clear();
                    _totalAvailableStock = 0;

                    foreach (DataRow r in dtLo.Rows)
                    {
                        int ton = 0;
                        int.TryParse(r["SOLUONGTON"].ToString(), out ton);
                        if (ton <= 0) continue;

                        _currentProductLots.Add(new LotInfo { MaLo = r["MALO"].ToString(), HSD = r["HSD"].ToString(), TonKho = ton });
                        _totalAvailableStock += ton;
                    }
                    lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
                }
                else
                {
                    lblTonKho.Text = "Nhập hàng mới";
                }
                txtSoLuong.Text = "0";
            }
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra dữ liệu đầu vào
            if (cboDoiTac.SelectedValue == null) { MessageBox.Show("Vui lòng chọn đối tác!"); return; }
            if (_tempItems.Count == 0) { MessageBox.Show("Chưa có sản phẩm!"); return; }

            // 2. Chuẩn bị dữ liệu
            string userStatus = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Đã thanh toán";
            bool isBoss = UserSession.CurrentUser != null && _highLevelRoles.Any(r => r.Equals(UserSession.CurrentUser.Chucvu, StringComparison.OrdinalIgnoreCase));
            int flag = isBoss ? 0 : 1;
            string finalStatus = isBoss ? userStatus : "Chờ duyệt";
            string note = txtGhiChu.Text + (isBoss ? "" : $" {{TrangThaiMongMuon:{userStatus}}}");
            bool isExport = rbXuat.IsChecked == true;
            decimal subTotal = _tempItems.Sum(x => x.ThanhTien);
            decimal finalTotal = subTotal * 1.1m;

            bool isSavedSuccess = false; // Biến cờ để kiểm tra lưu thành công hay chưa

            // 3. Thực hiện Giao dịch Lưu Hóa Đơn (Block 1)
            using (var conn = new SQLiteConnection(Database.ConnectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // A. Lưu Header
                        string sqlHead = isExport
                            ? "INSERT INTO HOADONXUAT (SOHDXUAT, NGAYLAP, TONGTIEN, VAT, MANV, MAKH, TRANGTHAI, GHICHU, PheDuyet) VALUES (@ma, @ngay, @tien, 10, @nv, @dt, @tt, @gc, @flag)"
                            : "INSERT INTO HOADONNHAP (SOHDNHAP, NGAYLAP, TONGTIEN, VAT, MANV, MANCC, TRANGTHAI, GHICHU, PheDuyet) VALUES (@ma, @ngay, @tien, 10, @nv, @dt, @tt, @gc, @flag)";

                        var cmdH = new SQLiteCommand(sqlHead, conn, trans);
                        cmdH.Parameters.AddWithValue("@ma", txtMaHD.Text);
                        cmdH.Parameters.AddWithValue("@ngay", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmdH.Parameters.AddWithValue("@tien", finalTotal);
                        cmdH.Parameters.AddWithValue("@nv", _currentMaNV);
                        cmdH.Parameters.AddWithValue("@dt", cboDoiTac.SelectedValue);
                        cmdH.Parameters.AddWithValue("@tt", finalStatus);
                        cmdH.Parameters.AddWithValue("@gc", note);
                        cmdH.Parameters.AddWithValue("@flag", flag);
                        cmdH.ExecuteNonQuery();

                        // B. Lưu Chi tiết
                        foreach (var item in _tempItems)
                        {
                            string sqlD = isExport
                                ? "INSERT INTO CTHDXUAT (SOHDXUAT, MASP, MALO, SOLUONG, DONGIABAN, THANHTIEN) VALUES (@mh, @msp, @ml, @sl, @gia, @tt)"
                                : "INSERT INTO CTHDNHAP (SOHDNHAP, MASP, MALO, SOLUONG, DONGIANHAP, THANHTIEN) VALUES (@mh, @msp, @ml, @sl, @gia, @tt)";

                            var cmdD = new SQLiteCommand(sqlD, conn, trans);
                            cmdD.Parameters.AddWithValue("@mh", txtMaHD.Text);
                            cmdD.Parameters.AddWithValue("@msp", item.MaSP);
                            cmdD.Parameters.AddWithValue("@ml", item.MaLo);
                            cmdD.Parameters.AddWithValue("@sl", item.SoLuong);
                            cmdD.Parameters.AddWithValue("@gia", item.DonGia);
                            cmdD.Parameters.AddWithValue("@tt", item.ThanhTien);
                            cmdD.ExecuteNonQuery();
                        }

                        trans.Commit(); // Lưu xong Database
                        isSavedSuccess = true; // Đánh dấu đã thành công
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback(); // Chỉ Rollback khi lỗi xảy ra trong quá trình Insert
                        MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.Message, "Lỗi SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                        return; // Dừng lại
                    }
                }
            } // <-- Tại đây kết nối Database cũ ĐÃ ĐÓNG HOÀN TOÀN

            // 4. Gửi yêu cầu sang kho (Block 2 - Chạy độc lập)
            // Chỉ chạy khi Block 1 đã thành công và đóng kết nối
            if (isSavedSuccess)
            {
                try
                {
                    WarehouseRequestService.GuiYeuCauTaoPhieuKho(txtMaHD.Text, isExport, cboDoiTac.SelectedValue.ToString(), _currentMaNV);

                    string msg = isBoss
                        ? "Đã lưu hóa đơn và gửi yêu cầu tạo phiếu kho thành công!"
                        : "Đã gửi yêu cầu phê duyệt hóa đơn!";

                    MessageBox.Show(msg, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    try { this.DialogResult = true; } catch { }
                    Close();
                }
                catch (Exception ex)
                {
                    // Nếu lỗi ở đây nghĩa là Hóa đơn đã lưu rồi, nhưng gửi kho bị lỗi
                    // Không được Rollback hóa đơn, chỉ báo lỗi cho user biết
                    MessageBox.Show("Hóa đơn đã được lưu, nhưng có lỗi khi gửi yêu cầu sang Kho:\n" + ex.Message, "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);

                    try { this.DialogResult = true; } catch { }
                    Close();
                }
            }
        }

        private void UpdateStockImmediately(InvoiceTempItem item, bool isExport, SQLiteConnection conn, SQLiteTransaction trans)
        {
            if (!isExport) // Nhập hàng
            {
                var cmdLo = new SQLiteCommand("INSERT OR IGNORE INTO LOHANG (MALO, MASP, NSX, HSD, NHACUNGCAP) VALUES (@ml, @msp, @nsx, @hsd, @ncc)", conn, trans);
                cmdLo.Parameters.AddWithValue("@ml", item.MaLo);
                cmdLo.Parameters.AddWithValue("@msp", item.MaSP);
                cmdLo.Parameters.AddWithValue("@nsx", item.NSX);
                cmdLo.Parameters.AddWithValue("@hsd", item.HSD);
                cmdLo.Parameters.AddWithValue("@ncc", cboDoiTac.SelectedValue);
                cmdLo.ExecuteNonQuery();

                var cmdTon = new SQLiteCommand(@"INSERT INTO TONKHO (MAKHO, MASP, MALO, SOLUONGTON) VALUES ('KHO_THUONG', @msp, @ml, @sl) 
                                                 ON CONFLICT(MAKHO, MASP, MALO) DO UPDATE SET SOLUONGTON = SOLUONGTON + @sl", conn, trans);
                cmdTon.Parameters.AddWithValue("@msp", item.MaSP);
                cmdTon.Parameters.AddWithValue("@ml", item.MaLo);
                cmdTon.Parameters.AddWithValue("@sl", item.SoLuong);
                cmdTon.ExecuteNonQuery();
            }
            else // Xuất hàng
            {
                var cmdTru = new SQLiteCommand("UPDATE TONKHO SET SOLUONGTON = SOLUONGTON - @sl WHERE MALO = @ml", conn, trans);
                cmdTru.Parameters.AddWithValue("@sl", item.SoLuong);
                cmdTru.Parameters.AddWithValue("@ml", item.MaLo);
                cmdTru.ExecuteNonQuery();
            }
        }

        private void btnXoaSP_Click(object sender, RoutedEventArgs e) { if ((sender as Button).DataContext is InvoiceTempItem item) { _tempItems.Remove(item); _addHistory.Clear(); CalculateTotal(); } }
        private void btnUndo_Click(object sender, RoutedEventArgs e) { if (_addHistory.Count > 0 && _tempItems.Count > 0) { int count = _addHistory.Pop(); for (int i = 0; i < count; i++) { if (_tempItems.Count > 0) { var last = _tempItems.Last(); if (rbXuat.IsChecked == true) { var lot = _currentProductLots.FirstOrDefault(l => l.MaLo == last.MaLo); if (lot != null) lot.TonKho += last.SoLuong; _totalAvailableStock += last.SoLuong; lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}"; } _tempItems.Remove(last); } } CalculateTotal(); } }
        private void CalculateTotal() { decimal sub = _tempItems.Sum(x => x.ThanhTien); decimal vat = sub * 0.1m; if (pnlVAT != null) { pnlVAT.Visibility = Visibility.Visible; txtVAT.Text = $"{vat:N0} VND"; } txtTienHang.Text = $"{sub:N0} VND"; txtTongCong.Text = $"{sub + vat:N0} VND"; }
        private void btnMinus_Click(object sender, RoutedEventArgs e) { if (int.TryParse(txtSoLuong.Text, out int q) && q > 0) txtSoLuong.Text = (q - 1).ToString(); }
        private void btnPlus_Click(object sender, RoutedEventArgs e) { if (int.TryParse(txtSoLuong.Text, out int q)) { if (rbXuat.IsChecked == true && q + 1 > _totalAvailableStock) { MessageBox.Show("Hết hàng"); return; } txtSoLuong.Text = (q + 1).ToString(); } }
        private void btnHuy_Click(object sender, RoutedEventArgs e) => Close();
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (!dgChiTiet.IsMouseOver) dgChiTiet.UnselectAll(); }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) => e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
    }
}