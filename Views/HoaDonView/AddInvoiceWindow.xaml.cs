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
    public class LotInfo { public string MaLo { get; set; } public string SoHieu { get; set; } public string HSD { get; set; } public int TonKho { get; set; } }

    public partial class AddInvoiceWindow : Window
    {
        private ObservableCollection<InvoiceTempItem> _tempItems = new ObservableCollection<InvoiceTempItem>();
        private List<LotInfo> _currentProductLots = new List<LotInfo>();
        private int _totalAvailableStock = 0;
        private string _currentMaNV = "";

        // [MỚI]: Biến lưu lịch sử để Hoàn tác thông minh (Stack lưu số lượng dòng đã thêm mỗi lần)
        private Stack<int> _addHistory = new Stack<int>();

        private readonly string[] _highLevelRoles = { "Admin", "Quản lý", "Giám đốc", "Kế toán" };

        public AddInvoiceWindow()
        {
            InitializeComponent();
            if (UserSession.IsLoggedIn)
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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!dgChiTiet.IsMouseOver) dgChiTiet.UnselectAll();
        }

        // --- NÚT HOÀN TÁC (ĐÃ NÂNG CẤP XÓA NHIỀU DÒNG) ---
        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem có lịch sử thao tác nào không
            if (_addHistory.Count > 0 && _tempItems.Count > 0)
            {
                // Lấy ra số lượng dòng cần xóa của thao tác gần nhất (ví dụ: 1 hoặc 20)
                int itemsToRemove = _addHistory.Pop();

                // Lặp để xóa đúng số lượng đó
                for (int i = 0; i < itemsToRemove; i++)
                {
                    if (_tempItems.Count == 0) break;

                    var lastItem = _tempItems.Last();

                    // Hoàn trả tồn kho (nếu là Xuất hàng)
                    if (rbXuat.IsChecked == true && cboSanPham.SelectedItem is DataRowView row)
                    {
                        // Chỉ hoàn trả nếu sản phẩm xóa trùng với sản phẩm đang chọn (để hiển thị đúng tồn kho)
                        if (lastItem.MaSP == row["MASP"].ToString())
                        {
                            var lot = _currentProductLots.FirstOrDefault(l => l.MaLo == lastItem.MaLo);
                            if (lot != null) lot.TonKho += lastItem.SoLuong;
                            _totalAvailableStock += lastItem.SoLuong;
                        }
                    }

                    // Xóa dòng cuối
                    _tempItems.Remove(lastItem);
                }

                // Cập nhật lại giao diện sau khi xóa xong cả cụm
                if (rbXuat.IsChecked == true) lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
                CalculateTotal();
            }
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (cboDoiTac.SelectedValue == null) { MessageBox.Show("Chưa chọn đối tác"); return; }
            if (_tempItems.Count == 0) { MessageBox.Show("Chưa có sản phẩm"); return; }

            string selectedStatus = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Đã thanh toán";
            string finalStatus = (UserSession.IsLoggedIn && _highLevelRoles.Contains(UserSession.CurrentUser.Chucvu)) ? selectedStatus : "Chờ duyệt";
            string note = txtGhiChu.Text + (finalStatus == "Chờ duyệt" ? $" {{TrangThaiMongMuon:{selectedStatus}}}" : "");

            bool isExport = rbXuat.IsChecked == true;
            decimal subTotal = _tempItems.Sum(x => x.ThanhTien);
            decimal finalTotal = subTotal * 1.1m;

            using (var conn = new SQLiteConnection("Data Source=PharmaDB.db"))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        string sqlHead = isExport
                            ? "INSERT INTO HOADONXUAT (SOHDXUAT, NGAYLAP, TONGTIEN, VAT, MANV, MAKH, TRANGTHAI, GHICHU) VALUES (@ma, @ngay, @tien, 10, @nv, @dt, @tt, @gc)"
                            : "INSERT INTO HOADONNHAP (SOHDNHAP, NGAYLAP, TONGTIEN, VAT, MANV, MANCC, TRANGTHAI, GHICHU) VALUES (@ma, @ngay, @tien, 10, @nv, @dt, @tt, @gc)";

                        var cmdH = new SQLiteCommand(sqlHead, conn);
                        cmdH.Parameters.AddWithValue("@ma", txtMaHD.Text);
                        cmdH.Parameters.AddWithValue("@ngay", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmdH.Parameters.AddWithValue("@tien", finalTotal);
                        cmdH.Parameters.AddWithValue("@nv", _currentMaNV);
                        cmdH.Parameters.AddWithValue("@dt", cboDoiTac.SelectedValue);
                        cmdH.Parameters.AddWithValue("@tt", finalStatus);
                        cmdH.Parameters.AddWithValue("@gc", note);
                        cmdH.ExecuteNonQuery();

                        foreach (var item in _tempItems)
                        {
                            if (!isExport)
                            {
                                var cmdLo = new SQLiteCommand("INSERT INTO LOHANG (MALO, MASP, SOHIEU, NSX, HSD, NHACUNGCAP) VALUES (@ml, @msp, @sh, @nsx, @hsd, @ncc)", conn);
                                cmdLo.Parameters.AddWithValue("@ml", item.MaLo); cmdLo.Parameters.AddWithValue("@msp", item.MaSP);
                                cmdLo.Parameters.AddWithValue("@sh", item.MaLo);
                                cmdLo.Parameters.AddWithValue("@nsx", item.NSX);
                                cmdLo.Parameters.AddWithValue("@hsd", item.HSD); cmdLo.Parameters.AddWithValue("@ncc", cboDoiTac.SelectedValue);
                                cmdLo.ExecuteNonQuery();

                                var cmdTon = new SQLiteCommand("INSERT INTO TONKHO (MAKHO, MASP, MALO, SOLUONGTON) VALUES ('KHO01', @msp, @ml, @sl)", conn);
                                cmdTon.Parameters.AddWithValue("@msp", item.MaSP); cmdTon.Parameters.AddWithValue("@ml", item.MaLo);
                                cmdTon.Parameters.AddWithValue("@sl", item.SoLuong);
                                cmdTon.ExecuteNonQuery();
                            }
                            else
                            {
                                var cmdTru = new SQLiteCommand("UPDATE TONKHO SET SOLUONGTON = SOLUONGTON - @sl WHERE MALO = @ml", conn);
                                cmdTru.Parameters.AddWithValue("@sl", item.SoLuong); cmdTru.Parameters.AddWithValue("@ml", item.MaLo);
                                cmdTru.ExecuteNonQuery();
                            }

                            string sqlD = isExport
                                ? "INSERT INTO CTHDXUAT (SOHDXUAT, MASP, MALO, SOLUONG, DONGIABAN, THANHTIEN) VALUES (@mh, @msp, @ml, @sl, @gia, @tt)"
                                : "INSERT INTO CTHDNHAP (SOHDNHAP, MASP, MALO, SOLUONG, DONGIANHAP, THANHTIEN) VALUES (@mh, @msp, @ml, @sl, @gia, @tt)";
                            var cmdD = new SQLiteCommand(sqlD, conn);
                            cmdD.Parameters.AddWithValue("@mh", txtMaHD.Text); cmdD.Parameters.AddWithValue("@msp", item.MaSP);
                            cmdD.Parameters.AddWithValue("@ml", item.MaLo); cmdD.Parameters.AddWithValue("@sl", item.SoLuong);
                            cmdD.Parameters.AddWithValue("@gia", item.DonGia); cmdD.Parameters.AddWithValue("@tt", item.ThanhTien);
                            cmdD.ExecuteNonQuery();
                        }
                        trans.Commit();
                        MessageBox.Show(finalStatus == "Chờ duyệt" ? "Đã gửi yêu cầu phê duyệt!" : "Tạo hóa đơn thành công!", "Thông báo");
                        Close();
                    }
                    catch (Exception ex) { trans.Rollback(); MessageBox.Show("Lỗi: " + ex.Message); }
                }
            }
        }

        private void InvoiceType_Checked(object sender, RoutedEventArgs e)
        {
            if (txtMaHD != null)
            {
                _tempItems.Clear();
                _addHistory.Clear(); // [QUAN TRỌNG] Xóa lịch sử khi đổi loại hóa đơn
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
            }
        }

        private void CalculateTotal() { decimal subTotal = _tempItems.Sum(x => x.ThanhTien); decimal vat = subTotal * 0.1m; decimal grandTotal = subTotal + vat; if (pnlVAT != null) { pnlVAT.Visibility = Visibility.Visible; txtVAT.Text = string.Format("{0:N0} VND", vat); } txtTienHang.Text = string.Format("{0:N0} VND", subTotal); txtTongCong.Text = string.Format("{0:N0} VND", grandTotal); }
        private void txtSoLoTach_TextChanged(object sender, TextChangedEventArgs e) { GenerateBatchCodesPreview(); }
        private void GenerateBatchCodesPreview() { if (txtSoLoTach == null || txtMaLoList == null) return; if (int.TryParse(txtSoLoTach.Text, out int count) && count > 0) { string timeStamp = DateTime.Now.ToString("yyMMddHHmm"); List<string> codes = new List<string>(); for (int i = 1; i <= count; i++) codes.Add($"[L{timeStamp}-{i}]"); txtMaLoList.Text = string.Join(", ", codes); } else txtMaLoList.Text = "(Nhập số lượng lô)"; }
        private void LoadProducts() { string sql = "SELECT MASP, TENSP AS TenSP, DVT AS DonVi, GIABAN AS DonGia FROM SANPHAM"; DataTable dt = Database.GetTable(sql); cboSanPham.ItemsSource = dt.DefaultView; }

        private void cboSanPham_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSanPham.SelectedItem is DataRowView row)
            {
                string maSP = row["MASP"].ToString();
                if (decimal.TryParse(row["DonGia"].ToString(), out decimal gia)) txtDonGia.Text = string.Format("{0:N0} VND", gia); else txtDonGia.Text = "0 VND";
                string sqlLo = $@"SELECT L.MALO, L.SOHIEU, L.HSD, IFNULL(T.SOLUONGTON, 0) AS SOLUONGTON FROM LOHANG L LEFT JOIN TONKHO T ON L.MALO=T.MALO WHERE L.MASP='{maSP}' ORDER BY L.HSD ASC"; DataTable dtLo = Database.GetTable(sqlLo); _currentProductLots.Clear(); _totalAvailableStock = 0; foreach (DataRow r in dtLo.Rows) { int ton = 0; int.TryParse(r["SOLUONGTON"].ToString(), out ton); if (rbXuat.IsChecked == true && ton <= 0) continue; _currentProductLots.Add(new LotInfo { MaLo = r["MALO"].ToString(), SoHieu = r["SOHIEU"].ToString(), HSD = r["HSD"].ToString(), TonKho = ton }); _totalAvailableStock += ton; }
                lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}"; txtSoLuong.Text = "0";
            }
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e) { if (int.TryParse(txtSoLuong.Text, out int qty) && qty > 0) txtSoLuong.Text = (qty - 1).ToString(); }
        private void btnPlus_Click(object sender, RoutedEventArgs e) { if (cboSanPham.SelectedItem == null) return; if (int.TryParse(txtSoLuong.Text, out int qty)) { if (rbXuat.IsChecked == true) { int nextTotal = qty + 1; if (nextTotal > _totalAvailableStock) { MessageBox.Show($"Không đủ hàng! Kho còn {_totalAvailableStock}"); return; } } txtSoLuong.Text = (qty + 1).ToString(); } }

        private void btnThemSP_Click(object sender, RoutedEventArgs e)
        {
            if (cboSanPham.SelectedItem == null) { MessageBox.Show("Chưa chọn sản phẩm!"); return; }

            int.TryParse(txtSoLuong.Text, out int slDat);
            string rawGia = txtDonGia.Text.Replace(" VND", "").Replace(",", "").Replace(".", "").Trim();
            decimal.TryParse(rawGia, out decimal donGia);

            if (slDat <= 0) { MessageBox.Show("Số lượng đặt phải > 0!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            int tongSL = slDat;
            var rowSP = cboSanPham.SelectedItem as DataRowView;
            string maSP = rowSP["MASP"].ToString();
            string tenSP = rowSP["TenSP"].ToString();
            string dvt = rowSP["DonVi"].ToString();

            if (rbNhap.IsChecked == true)
            {
                if (dpNSX.SelectedDate == null || dpHSD.SelectedDate == null) { MessageBox.Show("Chọn NSX/HSD!"); return; }
                if (dpHSD.SelectedDate <= dpNSX.SelectedDate) { MessageBox.Show("HSD phải sau NSX!"); return; }
                if (!int.TryParse(txtSoLoTach.Text, out int soLoTach) || soLoTach <= 0) soLoTach = 1;

                if (tongSL < soLoTach) { MessageBox.Show($"Tổng số lượng ({tongSL}) nhỏ hơn số lô cần tách ({soLoTach})!", "Lỗi chia lô", MessageBoxButton.OK, MessageBoxImage.Error); return; }

                // [QUAN TRỌNG] Ghi nhớ là lần này đã thêm bao nhiêu dòng
                _addHistory.Push(soLoTach);

                int slMoiLo = tongSL / soLoTach;
                int slDu = tongSL % soLoTach;
                string timeStamp = DateTime.Now.ToString("yyMMddHHmmss");
                for (int i = 0; i < soLoTach; i++)
                {
                    int slThucTe = slMoiLo;
                    if (i == soLoTach - 1) slThucTe += slDu;
                    string autoMaLo = $"L{timeStamp}-{i + 1}";
                    _tempItems.Add(new InvoiceTempItem { MaSP = maSP, TenSP = tenSP, DonVi = dvt, MaLo = autoMaLo, SoHieu = autoMaLo, NSX = dpNSX.SelectedDate.Value.ToString("yyyy-MM-dd"), HSD = dpHSD.SelectedDate.Value.ToString("yyyy-MM-dd"), SoLuong = slThucTe, DonGia = donGia });
                }
                CalculateTotal();
                txtSoLuong.Text = "0";
                txtSoLoTach.Text = "1";
                return;
            }

            if (tongSL > _totalAvailableStock) { MessageBox.Show($"Kho thiếu hàng! Chỉ còn {_totalAvailableStock}"); return; }

            // Nếu là Xuất hàng
            int canLay = tongSL;

            // [QUAN TRỌNG] Ghi nhớ lần này thêm 1 sản phẩm (dù có thể là nhiều lô gộp lại, nhưng trên giao diện thường là 1 dòng hoặc nhiều dòng tùy logic tách lô)
            // Trong logic dưới đây, Xuất hàng có thể sinh ra nhiều dòng nếu lấy từ nhiều lô khác nhau.
            // Nên ta cần đếm số lượng dòng thực tế được thêm vào list.
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
                        int i = _tempItems.IndexOf(exist);
                        _tempItems.RemoveAt(i);
                        _tempItems.Insert(i, exist);
                        // Nếu cộng dồn thì coi như không thêm dòng mới, nhưng để Undo được thao tác cộng dồn này phức tạp.
                        // Ở đây ta chấp nhận Undo chỉ xóa các dòng *mới thêm*.
                        // Tuy nhiên, logic Stack đơn giản nhất là: Nếu thêm mới -> Push 1. Nếu update -> Push 0 (hoặc xử lý riêng).
                        // Để đơn giản và an toàn: Ta coi mỗi lần bấm THÊM là 1 transaction.
                        // Nhưng vì bạn yêu cầu xóa "nhiều dòng", chủ yếu cho phần Nhập.
                        // Phần xuất này ta tạm thời Push(1) nếu thêm mới, Push(0) nếu update.
                    }
                    else
                    {
                        _tempItems.Add(new InvoiceTempItem { MaSP = maSP, TenSP = tenSP, DonVi = dvt, MaLo = lot.MaLo, SoHieu = lot.SoHieu, SoLuong = take, DonGia = donGia });
                        countAdded++;
                    }
                    canLay -= take;
                    lot.TonKho -= take;
                }
            }

            // Ghi nhớ số lượng dòng mới được sinh ra
            if (countAdded > 0) _addHistory.Push(countAdded);

            CalculateTotal();
            _totalAvailableStock -= tongSL;
            lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
            txtSoLuong.Text = "0";
        }

        // [SỬA LỖI LOGIC NÚT XÓA TAY]: Nếu xóa tay thì phải reset Stack để tránh lỗi Undo
        private void btnXoaSP_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is InvoiceTempItem item)
            {
                _tempItems.Remove(item);
                // Khi xóa tay bất kỳ dòng nào, Stack lịch sử sẽ bị sai lệch. 
                // Tốt nhất là xóa sạch lịch sử để tránh Undo nhầm.
                _addHistory.Clear();
                CalculateTotal();
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e) => Close();
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) => e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        private void LoadPartners(string table, string id, string name) { var dt = Database.GetTable($"SELECT {id}, {name} FROM {table}"); var list = new List<dynamic>(); foreach (DataRow r in dt.Rows) list.Add(new { Value = r[id], Display = $"{r[name]} - [{r[id]}]" }); cboDoiTac.ItemsSource = list; }
        private void GenerateInvoiceCode(string pre, string tbl, string col) { try { var dt = Database.GetTable($"SELECT {col} FROM {tbl} WHERE {col} LIKE '{pre}%' ORDER BY {col} DESC LIMIT 1"); if (dt.Rows.Count > 0) { string numPart = dt.Rows[0][0].ToString().Substring(pre.Length); txtMaHD.Text = int.TryParse(numPart, out int n) ? pre + (n + 1).ToString("D3") : pre + "001"; } else txtMaHD.Text = pre + "001"; } catch { txtMaHD.Text = pre + DateTime.Now.ToString("yyMMddHHmm"); } }
    }
}