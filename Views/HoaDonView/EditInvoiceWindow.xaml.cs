using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;
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

namespace PharmaDistributionApp.Views
{
    // --- CÁC CLASS HỖ TRỢ --
    public class EditProductItem
    {
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string DVT { get; set; }
        public decimal GIABAN { get; set; } // Sử dụng đúng tên cột GIABAN từ Database
    }

    public class EditLotInfo
    {
        public string MaLo { get; set; }
        public string HSD { get; set; }
        public int TonKho { get; set; }
    }

    public partial class EditInvoiceWindow : Window
    {
        private InvoiceViewModel _invoice;
        private ObservableCollection<EditCartItem> _tempItems = new ObservableCollection<EditCartItem>();
        private List<EditLotInfo> _currentProductLots = new List<EditLotInfo>();
        private int _totalAvailableStock = 0;
        private bool _isExport = true;
        private Stack<int> _addHistory = new Stack<int>();

        public EditInvoiceWindow(InvoiceViewModel invoice)
        {
            InitializeComponent();
            _invoice = invoice;
            _isExport = (_invoice.LoaiHD == "Xuất" || _invoice.MaHD.StartsWith("HDX", StringComparison.OrdinalIgnoreCase));

            InitUI();
            LoadData(); // Load chi tiết hóa đơn cũ
            LoadProducts(); // Load danh sách SP vào ComboBox
        }

        private void InitUI()
        {
            if (_isExport)
            {
                lblLoaiHD.Text = "HÓA ĐƠN XUẤT";
                bdLoaiHD.Background = (Brush)new BrushConverter().ConvertFrom("#E3F2FD");
                lblLoaiHD.Foreground = (Brush)new BrushConverter().ConvertFrom("#1565C0");
                lblDoiTac.Text = "Khách hàng";
                LoadPartners("KHACHHANG", "MAKH", "TENKH");
                pnlNhapHang.Visibility = Visibility.Collapsed;
                lblTonKho.Visibility = Visibility.Visible;
            }
            else
            {
                lblLoaiHD.Text = "HÓA ĐƠN NHẬP";
                bdLoaiHD.Background = (Brush)new BrushConverter().ConvertFrom("#FFF3E0");
                lblLoaiHD.Foreground = (Brush)new BrushConverter().ConvertFrom("#E65100");
                lblDoiTac.Text = "Nhà cung cấp";
                LoadPartners("NHACUNGCAP", "MANCC", "TENNCC");
                pnlNhapHang.Visibility = Visibility.Visible;
                lblTonKho.Visibility = Visibility.Collapsed;
                dpNSX.SelectedDate = DateTime.Now;
                dpHSD.SelectedDate = DateTime.Now.AddYears(2);
                txtSoLoTach.Text = "1";
            }

            txtMaHD.Text = _invoice.MaHD;
            txtNgayLap.Text = _invoice.NgayLap.ToString("dd/MM/yyyy HH:mm");

            // Chọn đúng trạng thái cũ
            foreach (ComboBoxItem item in cboTrangThai.Items)
            {
                if (item.Content.ToString() == _invoice.TrangThai) { cboTrangThai.SelectedItem = item; break; }
            }

            txtNhanVien.Text = UserSession.CurrentUser != null ? $"{UserSession.CurrentUser.Tennv} ({UserSession.CurrentUser.Manv})" : "Admin";
            dgChiTiet.ItemsSource = _tempItems;
        }

        // --- SỬA HÀM LOAD ĐỐI TÁC (3 THAM SỐ) ---
        private void LoadPartners(string table, string idCol, string nameCol)
        {
            try
            {
                var dt = Database.GetTable($"SELECT {idCol}, {nameCol} FROM {table}");
                var list = new List<dynamic>();
                foreach (DataRow r in dt.Rows)
                    list.Add(new { Value = r[idCol], Display = $"{r[nameCol]} - [{r[idCol]}]" });
                cboDoiTac.ItemsSource = list;
            }
            catch (Exception ex) { MessageBox.Show("Lỗi load đối tác: " + ex.Message); }
        }

        private void LoadProducts()
        {
            try
            {
                string sql = "SELECT MASP, TENSP, DVT, GIABAN FROM SANPHAM";
                DataTable dt = Database.GetTable(sql);
                cboSanPham.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex) { MessageBox.Show("Lỗi tải sản phẩm: " + ex.Message); }
        }

        private void LoadData()
        {
            try
            {
                string table = _isExport ? "HOADONXUAT" : "HOADONNHAP";
                string colID = _isExport ? "SOHDXUAT" : "SOHDNHAP";
                string colDT = _isExport ? "MAKH" : "MANCC";
                string tableCT = _isExport ? "CTHDXUAT" : "CTHDNHAP";
                string colGia = _isExport ? "DONGIABAN" : "DONGIANHAP";

                // Header
                var dtHead = Database.GetTable($"SELECT GHICHU, {colDT} FROM {table} WHERE {colID} = '{_invoice.MaHD}'");
                if (dtHead.Rows.Count > 0)
                {
                    txtGhiChu.Text = dtHead.Rows[0]["GHICHU"].ToString();
                    cboDoiTac.SelectedValue = dtHead.Rows[0][colDT].ToString();
                }

                // Details
                var dtDet = Database.GetTable($"SELECT CT.MASP, SP.TENSP, SP.DVT, CT.SOLUONG, CT.{colGia}, CT.MALO FROM {tableCT} CT LEFT JOIN SANPHAM SP ON CT.MASP = SP.MASP WHERE CT.{colID} = '{_invoice.MaHD}'");
                _tempItems.Clear();
                foreach (DataRow r in dtDet.Rows)
                {
                    _tempItems.Add(new EditCartItem
                    {
                        MaSP = r["MASP"].ToString(),
                        TenSP = r["TENSP"].ToString(),
                        DonVi = r["DVT"].ToString(),
                        SoLuong = Convert.ToInt32(r["SOLUONG"]),
                        DonGia = Convert.ToDecimal(r[colGia]),
                        ThanhTien = Convert.ToInt32(r["SOLUONG"]) * Convert.ToDecimal(r[colGia]),
                        MaLo = r["MALO"] != DBNull.Value ? r["MALO"].ToString() : ""
                    });
                }
                CalculateTotal();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi tải chi tiết: {ex.Message}"); }
        }

        private void cboSanPham_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSanPham.SelectedItem is DataRowView row)
            {
                string maSP = row["MASP"].ToString();
                decimal giaGoc = Convert.ToDecimal(row["GIABAN"]);
                decimal giaHienThi = _isExport ? giaGoc * 1.05m : giaGoc;
                txtDonGia.Text = string.Format("{0:N0} VND", giaHienThi);

                if (_isExport)
                {
                    var dt = Database.GetTable($@"SELECT L.MALO, L.HSD, IFNULL(T.SOLUONGTON, 0) AS SOLUONGTON FROM LOHANG L LEFT JOIN TONKHO T ON L.MALO=T.MALO WHERE L.MASP='{maSP}' ORDER BY L.HSD ASC");
                    _currentProductLots.Clear(); _totalAvailableStock = 0;
                    foreach (DataRow r in dt.Rows)
                    {
                        int t = Convert.ToInt32(r["SOLUONGTON"]);
                        if (t <= 0) continue;
                        _currentProductLots.Add(new EditLotInfo { MaLo = r["MALO"].ToString(), TonKho = t });
                        _totalAvailableStock += t;
                    }
                    lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
                }
                txtSoLuong.Text = "0";
                if (!_isExport) GenerateBatchCodesPreview();
            }
        }

        private void btnThemSP_Click(object sender, RoutedEventArgs e)
        {
            if (cboSanPham.SelectedItem == null) { MessageBox.Show("Chọn sản phẩm!"); return; }
            int.TryParse(txtSoLuong.Text, out int slDat);
            decimal donGia = decimal.Parse(txtDonGia.Text.Replace(" VND", "").Replace(",", "").Replace(".", ""));
            if (slDat <= 0) return;

            var row = cboSanPham.SelectedItem as DataRowView;
            string maSP = row["MASP"].ToString();
            string tenSP = row["TENSP"].ToString();
            string dvt = row["DVT"].ToString();

            if (!_isExport) // NHẬP HÀNG
            {
                if (dpNSX.SelectedDate == null || dpHSD.SelectedDate == null) return;
                int.TryParse(txtSoLoTach.Text, out int soLo); if (soLo <= 0) soLo = 1;
                List<string> codes = GetNextBatchCodes(soLo);
                _addHistory.Push(soLo);

                for (int i = 0; i < soLo; i++)
                {
                    int sl = (i == soLo - 1) ? (slDat / soLo + slDat % soLo) : (slDat / soLo);
                    _tempItems.Add(new EditCartItem { MaSP = maSP, TenSP = tenSP, DonVi = dvt, MaLo = codes[i], SoLuong = sl, DonGia = donGia, ThanhTien = sl * donGia });
                }
            }
            else // XUẤT HÀNG
            {
                if (slDat > _totalAvailableStock) { if (MessageBox.Show("Không đủ hàng, xuất âm?", "Cảnh báo", MessageBoxButton.YesNo) == MessageBoxResult.No) return; }
                int canLay = slDat; int added = 0;
                foreach (var lot in _currentProductLots)
                {
                    if (canLay <= 0) break;
                    int take = Math.Min(canLay, lot.TonKho);
                    if (take <= 0 && lot != _currentProductLots.Last()) continue;
                    if (lot == _currentProductLots.Last() && canLay > lot.TonKho) take = canLay;

                    _tempItems.Add(new EditCartItem { MaSP = maSP, TenSP = tenSP, DonVi = dvt, MaLo = lot.MaLo, SoLuong = take, DonGia = donGia, ThanhTien = take * donGia });
                    canLay -= take; added++;
                }
                _addHistory.Push(added);
            }
            CalculateTotal(); txtSoLuong.Text = "0";
        }
        // Khi thay đổi số lượng lô tách, hệ thống sẽ tự động tính toán và hiển thị mã lô dự kiến
        // --- HÀM LOGIC: TÌM MÃ LÔ TRỐNG (L001, L002...) ---
        // Hàm này quét Database và danh sách tạm để tìm các mã Lxxx chưa sử dụng

        // --- HÀM LOGIC MỚI: TÌM MÃ LÔ TRỐNG (L001, L002...) ---
        private List<string> GetNextBatchCodes(int countNeeded)
        {
            var usedNumbers = new HashSet<int>();
            var result = new List<string>();

            try
            {
                // 1. Lấy tất cả mã lô hiện có trong Database (bắt đầu bằng L)
                DataTable dt = Database.GetTable("SELECT MALO FROM LOHANG WHERE MALO LIKE 'L%'");
                foreach (DataRow row in dt.Rows)
                {
                    string dbCode = row["MALO"].ToString();
                    if (dbCode.Length > 1 && int.TryParse(dbCode.Substring(1), out int num))
                    {
                        usedNumbers.Add(num);
                    }
                }

                // 2. Lấy các mã lô đang nằm trong danh sách tạm trên lưới (chưa lưu)
                foreach (var item in _tempItems)
                {
                    if (!string.IsNullOrEmpty(item.MaLo) && item.MaLo.StartsWith("L"))
                    {
                        if (int.TryParse(item.MaLo.Substring(1), out int num))
                        {
                            usedNumbers.Add(num);
                        }
                    }
                }

                // 3. TÌM ĐỦ SỐ LƯỢNG MÃ TRỐNG
                int currentCheck = 1;
                while (result.Count < countNeeded)
                {
                    if (!usedNumbers.Contains(currentCheck))
                    {
                        result.Add($"L{currentCheck:D3}");
                        // QUAN TRỌNG: Thêm số vừa tìm được vào usedNumbers để vòng lặp sau không lấy trùng số này nữa
                        usedNumbers.Add(currentCheck);
                    }
                    currentCheck++;
                    if (currentCheck > 9999) break; // Giới hạn an toàn
                }
            }
            catch (Exception ex)
            {
                // Nếu lỗi DB, tạo mã dựa trên thời gian thực làm fallback
                for (int i = 0; i < countNeeded; i++)
                    result.Add($"L{DateTime.Now.ToString("ssfff")}{i}");
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

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (_addHistory.Count > 0)
            {
                int count = _addHistory.Pop();
                for (int i = 0; i < count; i++) if (_tempItems.Count > 0) _tempItems.Remove(_tempItems.Last());
                CalculateTotal();
            }
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Lưu thay đổi?", "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            bool success = false;
            using (var conn = new SQLiteConnection(Database.ConnectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        string tblCT = _isExport ? "CTHDXUAT" : "CTHDNHAP";
                        string tblHD = _isExport ? "HOADONXUAT" : "HOADONNHAP";
                        string colID = _isExport ? "SOHDXUAT" : "SOHDNHAP";
                        string colDT = _isExport ? "MAKH" : "MANCC";
                        string colGia = _isExport ? "DONGIABAN" : "DONGIANHAP";

                        new SQLiteCommand($"DELETE FROM {tblCT} WHERE {colID} = '{_invoice.MaHD}'", conn, trans).ExecuteNonQuery();

                        var cmdUp = new SQLiteCommand($"UPDATE {tblHD} SET TONGTIEN=@t, GHICHU=@g, TRANGTHAI=@s, {colDT}=@dt WHERE {colID}=@id", conn, trans);
                        cmdUp.Parameters.AddWithValue("@t", _tempItems.Sum(x => x.ThanhTien) * 1.1m);
                        cmdUp.Parameters.AddWithValue("@g", txtGhiChu.Text);
                        cmdUp.Parameters.AddWithValue("@s", (cboTrangThai.SelectedItem as ComboBoxItem).Content);
                        cmdUp.Parameters.AddWithValue("@dt", cboDoiTac.SelectedValue);
                        cmdUp.Parameters.AddWithValue("@id", _invoice.MaHD);
                        cmdUp.ExecuteNonQuery();

                        foreach (var item in _tempItems)
                        {
                            var cmdIns = new SQLiteCommand($"INSERT INTO {tblCT} ({colID}, MASP, MALO, SOLUONG, {colGia}, THANHTIEN) VALUES (@id, @sp, @ml, @sl, @gia, @tt)", conn, trans);
                            cmdIns.Parameters.AddWithValue("@id", _invoice.MaHD); cmdIns.Parameters.AddWithValue("@sp", item.MaSP);
                            cmdIns.Parameters.AddWithValue("@ml", item.MaLo); cmdIns.Parameters.AddWithValue("@sl", item.SoLuong);
                            cmdIns.Parameters.AddWithValue("@gia", item.DonGia); cmdIns.Parameters.AddWithValue("@tt", item.ThanhTien);
                            cmdIns.ExecuteNonQuery();
                        }
                        trans.Commit(); success = true;
                    }
                    catch (Exception ex) { trans.Rollback(); MessageBox.Show("Lỗi: " + ex.Message); }
                }
            }
            if (success)
            {
                WarehouseRequestService.GuiYeuCauTaoPhieuKho(_invoice.MaHD, _isExport, cboDoiTac.SelectedValue.ToString(), UserSession.CurrentUser?.Manv ?? "Admin");
                this.DialogResult = true; Close();
            }
        }

        private void CalculateTotal()
        {
            if (txtTienHang == null) return;
            decimal sum = _tempItems.Sum(x => x.ThanhTien);
            txtTienHang.Text = $"{sum:N0} VND"; lblTienVAT.Text = $"{sum * 0.1m:N0} VND"; txtTongCong.Text = $"{sum * 1.1m:N0} VND";
        }

        private void btnXoaSP_Click(object sender, RoutedEventArgs e) { if ((sender as Button).DataContext is EditCartItem i) { _tempItems.Remove(i); CalculateTotal(); } }
        private void btnMinus_Click(object sender, RoutedEventArgs e) { if (int.TryParse(txtSoLuong.Text, out int s) && s > 0) txtSoLuong.Text = (s - 1).ToString(); }
        private void btnPlus_Click(object sender, RoutedEventArgs e) { if (int.TryParse(txtSoLuong.Text, out int s)) txtSoLuong.Text = (s + 1).ToString(); }
        private void btnHuy_Click(object sender, RoutedEventArgs e) => Close();
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (!dgChiTiet.IsMouseOver) dgChiTiet.UnselectAll(); }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) => e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
    }
}