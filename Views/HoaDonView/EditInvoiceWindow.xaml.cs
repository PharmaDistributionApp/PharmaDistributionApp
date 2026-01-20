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
using System.Text.Json;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views
{
    // --- CÁC CLASS HỖ TRỢ --
    public class EditProductItem
    {
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string DVT { get; set; }
        public decimal GiaBan { get; set; }
        public decimal GiaNhap { get; set; }
        public int TonKho { get; set; }
    }

    public class EditLotItem
    {
        public string MaLo { get; set; }
        public string SoHieu { get; set; }
        public string HSD { get; set; }
        public int TonKho { get; set; }
    }

    public partial class EditInvoiceWindow : Window
    {
        private InvoiceViewModel _invoice;
        private ObservableCollection<EditCartItem> _tempItems = new ObservableCollection<EditCartItem>();
        private List<EditLotItem> _currentProductLots = new List<EditLotItem>();
        private int _totalAvailableStock = 0;
        private bool _isExport = true;
        private Stack<int> _addHistory = new Stack<int>();

        private readonly string[] _adminRoles = { "Admin", "Quản lý", "Giám đốc", "Kế toán", "ADMIN", "admin" };

        public EditInvoiceWindow(InvoiceViewModel invoice)
        {
            InitializeComponent();
            _invoice = invoice;
            _isExport = _invoice.LoaiHD == "Xuất" || _invoice.MaHD.StartsWith("HDX");
            InitUI();
            LoadData();
        }

        // --- HÀM NÀY CHỈ ĐƯỢC XUẤT HIỆN 1 LẦN ---
        private void EnsureRequestTableExists()
        {
            using (var conn = new SQLiteConnection("Data Source=PharmaDB.db"))
            {
                conn.Open();
                string sql = @"CREATE TABLE IF NOT EXISTS YEUCAU_SUA (
                                ID INTEGER PRIMARY KEY AUTOINCREMENT, 
                                MAHD TEXT, 
                                NOIDUNG_JSON TEXT, 
                                MANV TEXT, 
                                NGAYYEUCAU DATETIME, 
                                TRANGTHAI TEXT DEFAULT 'Chờ duyệt')";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();
            }
        }

        private void InitUI()
        {
            if (_isExport)
            {
                lblLoaiHD.Text = "HÓA ĐƠN XUẤT";
                bdLoaiHD.Background = (Brush)new BrushConverter().ConvertFrom("#E3F2FD");
                lblLoaiHD.Foreground = (Brush)new BrushConverter().ConvertFrom("#1565C0");
                lblDoiTac.Text = "Khách hàng";
            }
            else
            {
                lblLoaiHD.Text = "HÓA ĐƠN NHẬP";
                bdLoaiHD.Background = (Brush)new BrushConverter().ConvertFrom("#FFF3E0");
                lblLoaiHD.Foreground = (Brush)new BrushConverter().ConvertFrom("#E65100");
                lblDoiTac.Text = "Nhà cung cấp";
            }

            txtMaHD.Text = _invoice.MaHD;
            txtNgayLap.Text = _invoice.NgayLap.ToString("dd/MM/yyyy HH:mm");

            bool foundStatus = false;
            foreach (ComboBoxItem item in cboTrangThai.Items)
            {
                if (item.Content.ToString() == _invoice.TrangThai)
                {
                    cboTrangThai.SelectedItem = item;
                    foundStatus = true;
                    break;
                }
            }
            if (!foundStatus) cboTrangThai.SelectedIndex = 0;

            if (UserSession.CurrentUser != null)
                txtNhanVien.Text = $"{UserSession.CurrentUser.Tennv} ({UserSession.CurrentUser.Manv})";
            else
                txtNhanVien.Text = "Admin";

            dgChiTiet.ItemsSource = _tempItems;

            LoadPartners();
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                var dt = Database.GetTable("SELECT MASP, TENSP, DVT, GIABAN, GIANHAP FROM SANPHAM");
                BindProducts(dt);
            }
            catch
            {
                try
                {
                    var dt = Database.GetTable("SELECT MASP, TENSP, DVT, GIABAN, 0 AS GIANHAP FROM SANPHAM");
                    BindProducts(dt);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tải SP: {ex.Message}");
                }
            }
        }

        private void BindProducts(DataTable dt)
        {
            var list = new List<EditProductItem>();
            foreach (DataRow r in dt.Rows)
            {
                decimal gn = 0;
                if (dt.Columns.Contains("GIANHAP") && r["GIANHAP"] != DBNull.Value)
                    decimal.TryParse(r["GIANHAP"].ToString(), out gn);

                list.Add(new EditProductItem
                {
                    MaSP = r["MASP"].ToString(),
                    TenSP = r["TENSP"].ToString(),
                    DVT = r["DVT"].ToString(),
                    GiaBan = Convert.ToDecimal(r["GIABAN"]),
                    GiaNhap = gn,
                    TonKho = 0
                });
            }
            cboSanPham.ItemsSource = list;
        }

        private void LoadData()
        {
            try
            {
                string table = _isExport ? "HOADONXUAT" : "HOADONNHAP";
                string colID = _isExport ? "SOHDXUAT" : "SOHDNHAP";
                string colDT = _isExport ? "MAKH" : "MANCC";

                string sqlHead = $"SELECT GHICHU, {colDT} FROM {table} WHERE {colID} = '{_invoice.MaHD}'";
                var dtHead = Database.GetTable(sqlHead);
                if (dtHead.Rows.Count > 0)
                {
                    txtGhiChu.Text = dtHead.Rows[0]["GHICHU"].ToString();
                    cboDoiTac.SelectedValue = dtHead.Rows[0][colDT].ToString();
                }

                string tableCT = _isExport ? "CTHDXUAT" : "CTHDNHAP";
                string colGia = _isExport ? "DONGIABAN" : "DONGIANHAP";

                string sqlDet = $@"SELECT CT.MASP, SP.TENSP, SP.DVT, CT.SOLUONG, CT.{colGia}, CT.MALO 
                                   FROM {tableCT} CT LEFT JOIN SANPHAM SP ON CT.MASP = SP.MASP 
                                   WHERE CT.{colID} = '{_invoice.MaHD}'";

                var dtDet = Database.GetTable(sqlDet);
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
                        MaLo = r["MALO"] != DBNull.Value ? r["MALO"].ToString() : ""
                    });
                }
                CalculateTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết: {ex.Message}");
            }
        }

        private void LoadPartners()
        {
            string sql = _isExport ? "SELECT MAKH as Ma, TENKH as Ten FROM KHACHHANG" : "SELECT MANCC as Ma, TENNCC as Ten FROM NHACUNGCAP";
            var dt = Database.GetTable(sql);
            var list = new List<dynamic>();
            foreach (DataRow r in dt.Rows) list.Add(new { Value = r["Ma"], Display = $"{r["Ten"]} - [{r["Ma"]}]" });
            cboDoiTac.ItemsSource = list;
        }

        private void CalculateTotal()
        {
            if (txtTienHang == null) return;
            decimal sum = _tempItems.Sum(x => x.ThanhTien);
            decimal vat = sum * 0.1m;
            txtTienHang.Text = $"{sum:N0} VND";
            lblTienVAT.Text = $"{vat:N0} VND";
            txtTongCong.Text = $"{sum + vat:N0} VND";
        }

        private void cboSanPham_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSanPham.SelectedItem is EditProductItem p)
            {
                decimal gia = _isExport ? p.GiaBan : p.GiaNhap;
                txtDonGia.Text = string.Format("{0:N0} VND", gia);

                string sqlLo = $@"SELECT L.MALO, L.SOHIEU, L.HSD, IFNULL(T.SOLUONGTON, 0) AS SOLUONGTON 
                                  FROM LOHANG L LEFT JOIN TONKHO T ON L.MALO=T.MALO 
                                  WHERE L.MASP='{p.MaSP}' ORDER BY L.HSD ASC";
                var dt = Database.GetTable(sqlLo);
                _currentProductLots.Clear();
                _totalAvailableStock = 0;
                foreach (DataRow r in dt.Rows)
                {
                    int t = Convert.ToInt32(r["SOLUONGTON"]);
                    if (_isExport && t <= 0) continue;
                    _currentProductLots.Add(new EditLotItem { MaLo = r["MALO"].ToString(), TonKho = t });
                    _totalAvailableStock += t;
                }
                lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
            }
        }

        private void btnThemSP_Click(object sender, RoutedEventArgs e)
        {
            if (cboSanPham.SelectedItem == null) { MessageBox.Show("Chọn sản phẩm!"); return; }
            int.TryParse(txtSoLuong.Text, out int sl);
            string rawGia = txtDonGia.Text.Replace(" VND", "").Replace(",", "").Replace(".", "").Trim();
            decimal.TryParse(rawGia, out decimal gia);

            if (sl <= 0) { MessageBox.Show("Số lượng > 0"); return; }

            var p = cboSanPham.SelectedItem as EditProductItem;
            if (_isExport && sl > _totalAvailableStock) { MessageBox.Show($"Không đủ hàng! Kho còn: {_totalAvailableStock}"); return; }

            int canLay = sl;
            int addedCount = 0;

            foreach (var lot in _currentProductLots)
            {
                if (canLay <= 0) break;
                int take = Math.Min(canLay, lot.TonKho);
                var exist = _tempItems.FirstOrDefault(x => x.MaSP == p.MaSP && x.MaLo == lot.MaLo);
                if (exist != null) { exist.SoLuong += take; }
                else
                {
                    _tempItems.Add(new EditCartItem { MaSP = p.MaSP, TenSP = p.TenSP, DonVi = p.DVT, SoLuong = take, DonGia = gia, MaLo = lot.MaLo });
                    addedCount++;
                }
                canLay -= take;
                lot.TonKho -= take;
            }
            _addHistory.Push(addedCount > 0 ? addedCount : 1);
            CalculateTotal();
            txtSoLuong.Text = "0";
            _totalAvailableStock -= sl;
            lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (_addHistory.Count > 0 && _tempItems.Count > 0)
            {
                int count = _addHistory.Pop();
                for (int i = 0; i < count; i++)
                {
                    if (_tempItems.Count == 0) break;
                    var item = _tempItems.Last();
                    var lot = _currentProductLots.FirstOrDefault(l => l.MaLo == item.MaLo);
                    if (lot != null) { lot.TonKho += item.SoLuong; _totalAvailableStock += item.SoLuong; }
                    _tempItems.Remove(item);
                }
                if (cboSanPham.SelectedItem != null) lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
                CalculateTotal();
            }
        }

        private void btnXoaSP_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is EditCartItem i)
            {
                _tempItems.Remove(i);
                _addHistory.Clear();
                CalculateTotal();
            }
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            // Sếp lưu trực tiếp, cập nhật ngay
            if (MessageBox.Show("Lưu thay đổi vào Database?", "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            using (var conn = new SQLiteConnection("Data Source=PharmaDB.db"))
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

                        // 1. Hoàn kho cũ (chỉ hoàn nếu đơn đã được duyệt/trừ kho trước đó)
                        // Nếu sửa đơn treo (PheDuyet=1) thì không cần hoàn
                        int curFlag = 0;
                        var cmdCheck = new SQLiteCommand($"SELECT PheDuyet FROM {tblHD} WHERE {colID}='{_invoice.MaHD}'", conn, trans);
                        object objFlag = cmdCheck.ExecuteScalar();
                        if (objFlag != null) curFlag = Convert.ToInt32(objFlag);

                        if (curFlag == 0) // Đơn chính thức -> Phải hoàn kho
                        {
                            string opOld = _isExport ? "+" : "-";
                            string sqlOld = $"SELECT MALO, SOLUONG FROM {tblCT} WHERE {colID} = '{_invoice.MaHD}'";
                            using (var cmdOld = new SQLiteCommand(sqlOld, conn, trans))
                            using (var r = cmdOld.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    new SQLiteCommand($"UPDATE TONKHO SET SOLUONGTON = SOLUONGTON {opOld} {r["SOLUONG"]} WHERE MALO='{r["MALO"]}'", conn, trans).ExecuteNonQuery();
                                }
                            }
                        }

                        // 2. Xóa chi tiết cũ
                        new SQLiteCommand($"DELETE FROM {tblCT} WHERE {colID} = '{_invoice.MaHD}'", conn, trans).ExecuteNonQuery();

                        // 3. Update Header (Chuyển luôn thành Đã thanh toán, PheDuyet=0)
                        string sqlUp = $"UPDATE {tblHD} SET TONGTIEN=@t, VAT=10, GHICHU=@g, TRANGTHAI='Đã thanh toán', PheDuyet=0, {colDT}=@dt WHERE {colID}=@id";
                        var cmdUp = new SQLiteCommand(sqlUp, conn, trans);
                        cmdUp.Parameters.AddWithValue("@t", _tempItems.Sum(x => x.ThanhTien));
                        cmdUp.Parameters.AddWithValue("@g", txtGhiChu.Text);
                        cmdUp.Parameters.AddWithValue("@dt", cboDoiTac.SelectedValue);
                        cmdUp.Parameters.AddWithValue("@id", _invoice.MaHD);
                        cmdUp.ExecuteNonQuery();

                        // 4. Insert Mới & Trừ kho Mới
                        string colGia = _isExport ? "DONGIABAN" : "DONGIANHAP";
                        string opNew = _isExport ? "-" : "+";

                        foreach (var item in _tempItems)
                        {
                            var cmdIns = new SQLiteCommand($"INSERT INTO {tblCT} ({colID}, MASP, MALO, SOLUONG, {colGia}, THANHTIEN) VALUES (@id, @sp, @ml, @sl, @gia, @tt)", conn, trans);
                            cmdIns.Parameters.AddWithValue("@id", _invoice.MaHD);
                            cmdIns.Parameters.AddWithValue("@sp", item.MaSP);
                            cmdIns.Parameters.AddWithValue("@ml", item.MaLo);
                            cmdIns.Parameters.AddWithValue("@sl", item.SoLuong);
                            cmdIns.Parameters.AddWithValue("@gia", item.DonGia);
                            cmdIns.Parameters.AddWithValue("@tt", item.ThanhTien);
                            cmdIns.ExecuteNonQuery();

                            // Trừ kho luôn
                            new SQLiteCommand($"UPDATE TONKHO SET SOLUONGTON = SOLUONGTON {opNew} {item.SoLuong} WHERE MALO='{item.MaLo}'", conn, trans).ExecuteNonQuery();
                        }

                        trans.Commit();
                        MessageBox.Show("Cập nhật thành công!");
                        try { this.DialogResult = true; } catch { }
                        Close();
                    }
                    catch (Exception ex) { trans.Rollback(); MessageBox.Show("Lỗi: " + ex.Message); }
                }
            }
        }

        private void SendRequestAsEmployee()
        {
            if (MessageBox.Show("Gửi yêu cầu chỉnh sửa?", "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

            try
            {
                var requestData = new InvoiceEditRequestModel
                {
                    TongTien = _tempItems.Sum(x => x.ThanhTien),
                    VAT = 10,
                    GhiChu = txtGhiChu.Text,
                    TrangThai = (cboTrangThai.SelectedItem as ComboBoxItem).Content.ToString(),
                    MaDoiTac = cboDoiTac.SelectedValue?.ToString(),
                    ChiTiet = _tempItems.ToList()
                };

                string json = JsonSerializer.Serialize(requestData);
                string curNV = UserSession.CurrentUser?.Manv ?? "Unk";

                using (var conn = new SQLiteConnection("Data Source=PharmaDB.db"))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        var cmd = new SQLiteCommand("INSERT INTO YEUCAU_SUA (MAHD, NOIDUNG_JSON, MANV, NGAYYEUCAU) VALUES (@mh, @js, @nv, @dt)", conn, trans);
                        cmd.Parameters.AddWithValue("@mh", _invoice.MaHD);
                        cmd.Parameters.AddWithValue("@js", json);
                        cmd.Parameters.AddWithValue("@nv", curNV);
                        cmd.Parameters.AddWithValue("@dt", DateTime.Now);
                        cmd.ExecuteNonQuery();

                        // Set PheDuyet = 2 (Chờ sửa)
                        string tbl = _isExport ? "HOADONXUAT" : "HOADONNHAP";
                        string col = _isExport ? "SOHDXUAT" : "SOHDNHAP";
                        new SQLiteCommand($"UPDATE {tbl} SET PheDuyet = 2 WHERE {col} = '{_invoice.MaHD}'", conn, trans).ExecuteNonQuery();

                        trans.Commit();
                    }
                }
                MessageBox.Show("Đã gửi yêu cầu chỉnh sửa.");
                try { this.DialogResult = true; } catch { }
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi gửi yêu cầu: " + ex.Message); }
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e) { if (int.TryParse(txtSoLuong.Text, out int s) && s > 0) txtSoLuong.Text = (s - 1).ToString(); }
        private void btnPlus_Click(object sender, RoutedEventArgs e) { if (int.TryParse(txtSoLuong.Text, out int s)) txtSoLuong.Text = (s + 1).ToString(); }
        private void btnHuy_Click(object sender, RoutedEventArgs e) => Close();
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (!dgChiTiet.IsMouseOver) dgChiTiet.UnselectAll(); }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) => e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
    }
}