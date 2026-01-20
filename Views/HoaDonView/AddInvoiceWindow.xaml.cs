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
        private Stack<int> _addHistory = new Stack<int>(); // Stack để hoàn tác (Undo)

        // Danh sách quyền Admin/Quản lý
        private readonly string[] _highLevelRoles = { "Admin", "Quản lý", "Giám đốc", "Kế toán" };

        public AddInvoiceWindow()
        {
            InitializeComponent();

            // Lấy thông tin người dùng hiện tại
            if (UserSession.IsLoggedIn && UserSession.CurrentUser != null)
            {
                _currentMaNV = UserSession.CurrentUser.Manv;
                txtNhanVien.Text = $"{UserSession.CurrentUser.Tennv} ({_currentMaNV})";
            }
            else
            {
                _currentMaNV = "NV001";
                txtNhanVien.Text = "Admin (NV001)";
            }

            txtNgayLap.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            dgChiTiet.ItemsSource = _tempItems;

            LoadProducts();
            InvoiceType_Checked(null, null); // Khởi tạo giao diện theo loại hóa đơn mặc định
        }

        // --- SỰ KIỆN CHỌN LOẠI HÓA ĐƠN ---
        private void InvoiceType_Checked(object sender, RoutedEventArgs e)
        {
            if (txtMaHD == null) return;

            // Reset dữ liệu
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

                // Ẩn panel nhập hàng (NSX, HSD...)
                pnlNhapHang.Visibility = Visibility.Collapsed;
                lblTonKho.Visibility = Visibility.Visible;
            }
            else
            {
                lblDoiTac.Text = "Nhà cung cấp";
                GenerateInvoiceCode("HDN", "HOADONNHAP", "SOHDNHAP");
                LoadPartners("NHACUNGCAP", "MANCC", "TENNCC");

                txtDonGia.Text = "0 VND";

                // Hiện panel nhập hàng
                pnlNhapHang.Visibility = Visibility.Visible;
                lblTonKho.Visibility = Visibility.Collapsed;

                // Mặc định ngày
                dpNSX.SelectedDate = DateTime.Now;
                dpHSD.SelectedDate = DateTime.Now.AddYears(2);
                txtSoLoTach.Text = "1";
                GenerateBatchCodesPreview();
            }
        }

        // --- CÁC HÀM TẢI DỮ LIỆU ---
        private void LoadProducts()
        {
            string sql = "SELECT MASP, TENSP AS TenSP, DVT AS DonVi, GIABAN AS DonGia FROM SANPHAM";
            DataTable dt = Database.GetTable(sql);
            cboSanPham.ItemsSource = dt.DefaultView;
        }

        private void LoadPartners(string table, string idCol, string nameCol)
        {
            var dt = Database.GetTable($"SELECT {idCol}, {nameCol} FROM {table}");
            var list = new List<dynamic>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new { Value = r[idCol], Display = $"{r[nameCol]} - [{r[idCol]}]" });
            }
            cboDoiTac.ItemsSource = list;
        }

        private void GenerateInvoiceCode(string prefix, string table, string col)
        {
            try
            {
                var dt = Database.GetTable($"SELECT {col} FROM {table} WHERE {col} LIKE '{prefix}%' ORDER BY {col} DESC LIMIT 1");
                if (dt.Rows.Count > 0)
                {
                    string lastCode = dt.Rows[0][0].ToString();
                    string numPart = lastCode.Substring(prefix.Length);
                    if (int.TryParse(numPart, out int n))
                    {
                        txtMaHD.Text = prefix + (n + 1).ToString("D3");
                    }
                    else
                    {
                        txtMaHD.Text = prefix + "001";
                    }
                }
                else
                {
                    txtMaHD.Text = prefix + "001";
                }
            }
            catch
            {
                txtMaHD.Text = prefix + DateTime.Now.ToString("yyMMddHHmm");
            }
        }

        // --- XỬ LÝ CHỌN SẢN PHẨM ---
        private void cboSanPham_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSanPham.SelectedItem is DataRowView row)
            {
                string maSP = row["MASP"].ToString();

                if (decimal.TryParse(row["DonGia"].ToString(), out decimal gia))
                    txtDonGia.Text = string.Format("{0:N0} VND", gia);
                else
                    txtDonGia.Text = "0 VND";

                // Lấy thông tin lô hàng và tồn kho của sản phẩm này
                string sqlLo = $@"SELECT L.MALO, L.HSD, IFNULL(T.SOLUONGTON, 0) AS SOLUONGTON 
                                  FROM LOHANG L 
                                  LEFT JOIN TONKHO T ON L.MALO = T.MALO 
                                  WHERE L.MASP = '{maSP}' 
                                  ORDER BY L.HSD ASC"; // Ưu tiên HSD gần nhất (FEFO)

                DataTable dtLo = Database.GetTable(sqlLo);
                _currentProductLots.Clear();
                _totalAvailableStock = 0;

                foreach (DataRow r in dtLo.Rows)
                {
                    int ton = 0;
                    int.TryParse(r["SOLUONGTON"].ToString(), out ton);

                    // Nếu là Xuất hàng, chỉ lấy lô còn tồn
                    if (rbXuat.IsChecked == true && ton <= 0) continue;

                    _currentProductLots.Add(new LotInfo
                    {
                        MaLo = r["MALO"].ToString(),
                        HSD = r["HSD"].ToString(),
                        TonKho = ton
                    });

                    _totalAvailableStock += ton;
                }

                lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
                txtSoLuong.Text = "0";
            }
        }

        // --- XỬ LÝ THÊM SẢN PHẨM VÀO LƯỚI ---
        private void btnThemSP_Click(object sender, RoutedEventArgs e)
        {
            if (cboSanPham.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm!");
                return;
            }

            int.TryParse(txtSoLuong.Text, out int slDat);

            // Parse đơn giá từ textbox (bỏ chữ VND và dấu phẩy)
            string rawGia = txtDonGia.Text.Replace(" VND", "").Replace(",", "").Replace(".", "").Trim();
            decimal.TryParse(rawGia, out decimal donGia);

            if (slDat <= 0)
            {
                MessageBox.Show("Số lượng phải lớn hơn 0!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int tongSL = slDat;
            var rowSP = cboSanPham.SelectedItem as DataRowView;
            string maSP = rowSP["MASP"].ToString();
            string tenSP = rowSP["TenSP"].ToString();
            string dvt = rowSP["DonVi"].ToString();

            // === TRƯỜNG HỢP 1: NHẬP HÀNG (Tạo lô mới) ===
            if (rbNhap.IsChecked == true)
            {
                if (dpNSX.SelectedDate == null || dpHSD.SelectedDate == null)
                {
                    MessageBox.Show("Vui lòng chọn NSX và HSD!");
                    return;
                }

                if (dpHSD.SelectedDate <= dpNSX.SelectedDate)
                {
                    MessageBox.Show("Hạn sử dụng phải sau Ngày sản xuất!");
                    return;
                }

                if (!int.TryParse(txtSoLoTach.Text, out int soLoTach) || soLoTach <= 0) soLoTach = 1;

                if (tongSL < soLoTach)
                {
                    MessageBox.Show($"Tổng số lượng ({tongSL}) nhỏ hơn số lô cần tách ({soLoTach})!", "Lỗi chia lô", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Lưu số lượng item sẽ thêm để phục vụ Undo
                _addHistory.Push(soLoTach);

                int slMoiLo = tongSL / soLoTach;
                int slDu = tongSL % soLoTach;
                string timeStamp = DateTime.Now.ToString("yyMMddHHmmss");

                for (int i = 0; i < soLoTach; i++)
                {
                    int slThucTe = slMoiLo;
                    if (i == soLoTach - 1) slThucTe += slDu; // Cộng phần dư vào lô cuối

                    string autoMaLo = $"L{timeStamp}-{i + 1}";

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

            // === TRƯỜNG HỢP 2: XUẤT HÀNG (Trừ kho tự động theo lô) ===
            if (tongSL > _totalAvailableStock)
            {
                MessageBox.Show($"Kho không đủ hàng! Chỉ còn {_totalAvailableStock} {dvt}");
                return;
            }

            int canLay = tongSL;
            int countAdded = 0;

            // Duyệt qua từng lô (đã sort theo HSD tăng dần) để trừ dần
            foreach (var lot in _currentProductLots)
            {
                if (canLay <= 0) break;

                if (lot.TonKho > 0)
                {
                    int take = Math.Min(canLay, lot.TonKho);

                    // Kiểm tra xem lô này đã có trong danh sách tạm chưa
                    var exist = _tempItems.FirstOrDefault(x => x.MaSP == maSP && x.MaLo == lot.MaLo);

                    if (exist != null)
                    {
                        // Nếu có rồi thì cộng dồn số lượng
                        exist.SoLuong += take;

                        // Trick để DataGrid cập nhật lại hiển thị
                        int i = _tempItems.IndexOf(exist);
                        _tempItems.RemoveAt(i);
                        _tempItems.Insert(i, exist);
                    }
                    else
                    {
                        // Nếu chưa có thì thêm mới
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
                    lot.TonKho -= take; // Trừ kho ảo trong danh sách lô hiện tại
                }
            }

            if (countAdded > 0) _addHistory.Push(countAdded);

            CalculateTotal();
            _totalAvailableStock -= tongSL; // Cập nhật tổng tồn hiển thị
            lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
            txtSoLuong.Text = "0";
        }

        // --- NÚT LƯU & GỬI DUYỆT ---
        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (cboDoiTac.SelectedValue == null) { MessageBox.Show("Vui lòng chọn đối tác!"); return; }
            if (_tempItems.Count == 0) { MessageBox.Show("Chưa có sản phẩm nào trong danh sách!"); return; }

            string userStatus = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Đã thanh toán";

            // Kiểm tra quyền Sếp
            bool isBoss = UserSession.CurrentUser != null && _highLevelRoles.Any(r => r.Equals(UserSession.CurrentUser.Chucvu, StringComparison.OrdinalIgnoreCase));

            // LOGIC QUAN TRỌNG:
            // Sếp: PheDuyet = 0 (Hiển thị ngay), TrangThai = Đã thanh toán.
            // Nhân viên: PheDuyet = 1 (Chờ duyệt - Ẩn ở bảng chính), TrangThai = Chờ duyệt.
            int flag = isBoss ? 0 : 1;
            string finalStatus = isBoss ? userStatus : "Chờ duyệt";

            // Nếu là nhân viên, lưu trạng thái mong muốn vào ghi chú để sếp biết
            string note = txtGhiChu.Text + (isBoss ? "" : $" {{TrangThaiMongMuon:{userStatus}}}");

            bool isExport = rbXuat.IsChecked == true;
            decimal subTotal = _tempItems.Sum(x => x.ThanhTien);
            decimal finalTotal = subTotal * 1.1m; // VAT 10%

            using (var conn = new SQLiteConnection("Data Source=PharmaDB.db"))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Lưu Header Hóa Đơn
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

                        // 2. Lưu Chi Tiết
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

                            // 3. QUAN TRỌNG: Cập nhật kho
                            // - Nếu là Sếp: Trừ kho ngay lập tức.
                            // - Nếu là Nhân viên: Chỉ lưu dữ liệu, CHƯA trừ kho (đợi Sếp duyệt).
                            if (isBoss)
                            {
                                UpdateStockImmediately(item, isExport, conn, trans);
                            }
                        }

                        trans.Commit();

                        string msg = isBoss ? "Tạo hóa đơn thành công!" : "Đã gửi yêu cầu tạo mới! Hóa đơn hiện đang ở trạng thái 'Chờ duyệt'.";
                        MessageBox.Show(msg);

                        try { this.DialogResult = true; } catch { }
                        Close();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        MessageBox.Show("Lỗi lưu dữ liệu: " + ex.Message);
                    }
                }
            }
        }

        // Hàm cập nhật kho ngay lập tức (Dành cho Sếp)
        private void UpdateStockImmediately(InvoiceTempItem item, bool isExport, SQLiteConnection conn, SQLiteTransaction trans)
        {
            if (!isExport) // Nhập hàng
            {
                // Thêm lô mới nếu chưa có
                var cmdLo = new SQLiteCommand("INSERT OR IGNORE INTO LOHANG (MALO, MASP, NSX, HSD, NHACUNGCAP) VALUES (@ml, @msp, @sh, @nsx, @hsd, @ncc)", conn, trans);
                cmdLo.Parameters.AddWithValue("@ml", item.MaLo);
                cmdLo.Parameters.AddWithValue("@msp", item.MaSP);
                cmdLo.Parameters.AddWithValue("@sh", item.MaLo);
                cmdLo.Parameters.AddWithValue("@nsx", item.NSX);
                cmdLo.Parameters.AddWithValue("@hsd", item.HSD);
                cmdLo.Parameters.AddWithValue("@ncc", cboDoiTac.SelectedValue);
                cmdLo.ExecuteNonQuery();

                // Cộng tồn kho
                var cmdTon = new SQLiteCommand(@"INSERT INTO TONKHO (MAKHO, MASP, MALO, SOLUONGTON) VALUES ('KHO01', @msp, @ml, @sl) 
                                                 ON CONFLICT(MAKHO, MASP, MALO) DO UPDATE SET SOLUONGTON = SOLUONGTON + @sl", conn, trans);
                cmdTon.Parameters.AddWithValue("@msp", item.MaSP);
                cmdTon.Parameters.AddWithValue("@ml", item.MaLo);
                cmdTon.Parameters.AddWithValue("@sl", item.SoLuong);
                cmdTon.ExecuteNonQuery();
            }
            else // Xuất hàng
            {
                // Trừ tồn kho
                var cmdTru = new SQLiteCommand("UPDATE TONKHO SET SOLUONGTON = SOLUONGTON - @sl WHERE MALO = @ml", conn, trans);
                cmdTru.Parameters.AddWithValue("@sl", item.SoLuong);
                cmdTru.Parameters.AddWithValue("@ml", item.MaLo);
                cmdTru.ExecuteNonQuery();
            }
        }

        // --- CÁC HÀM XỬ LÝ GIAO DIỆN KHÁC ---

        private void btnXoaSP_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is InvoiceTempItem item)
            {
                _tempItems.Remove(item);

                // Nếu xóa tay từng dòng thì xóa luôn lịch sử Undo để tránh lỗi logic
                _addHistory.Clear();

                // Nếu là hóa đơn xuất, cần hoàn trả lại số lượng vào biến _totalAvailableStock (ảo)
                if (rbXuat.IsChecked == true)
                {
                    // Logic này hơi phức tạp nếu muốn hoàn chính xác vào từng lô
                    // Nên đơn giản nhất là yêu cầu user chọn lại sản phẩm để load lại kho
                    // Ở đây ta chỉ cập nhật lại Tổng tiền
                }

                CalculateTotal();
            }
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (_addHistory.Count > 0 && _tempItems.Count > 0)
            {
                int itemsToRemove = _addHistory.Pop();
                for (int i = 0; i < itemsToRemove; i++)
                {
                    if (_tempItems.Count == 0) break;
                    var lastItem = _tempItems.Last();

                    // Nếu là xuất hàng, hoàn trả lại kho ảo
                    if (rbXuat.IsChecked == true && cboSanPham.SelectedItem is DataRowView row)
                    {
                        if (lastItem.MaSP == row["MASP"].ToString())
                        {
                            var lot = _currentProductLots.FirstOrDefault(l => l.MaLo == lastItem.MaLo);
                            if (lot != null) lot.TonKho += lastItem.SoLuong;
                            _totalAvailableStock += lastItem.SoLuong;
                        }
                    }
                    _tempItems.Remove(lastItem);
                }

                if (rbXuat.IsChecked == true) lblTonKho.Text = $"Tổng tồn: {_totalAvailableStock:N0}";
                CalculateTotal();
            }
        }

        private void CalculateTotal()
        {
            decimal subTotal = _tempItems.Sum(x => x.ThanhTien);
            decimal vat = subTotal * 0.1m;
            decimal grandTotal = subTotal + vat;

            if (pnlVAT != null)
            {
                pnlVAT.Visibility = Visibility.Visible;
                txtVAT.Text = string.Format("{0:N0} VND", vat);
            }

            txtTienHang.Text = string.Format("{0:N0} VND", subTotal);
            txtTongCong.Text = string.Format("{0:N0} VND", grandTotal);
        }

        private void txtSoLoTach_TextChanged(object sender, TextChangedEventArgs e)
        {
            GenerateBatchCodesPreview();
        }

        private void GenerateBatchCodesPreview()
        {
            if (txtSoLoTach == null || txtMaLoList == null) return;

            if (int.TryParse(txtSoLoTach.Text, out int count) && count > 0)
            {
                string timeStamp = DateTime.Now.ToString("yyMMddHHmm");
                List<string> codes = new List<string>();
                for (int i = 1; i <= count; i++)
                {
                    codes.Add($"[L{timeStamp}-{i}]");
                }
                txtMaLoList.Text = string.Join(", ", codes);
            }
            else
            {
                txtMaLoList.Text = "(Nhập số lượng lô)";
            }
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtSoLuong.Text, out int qty) && qty > 0)
            {
                txtSoLuong.Text = (qty - 1).ToString();
            }
        }

        private void btnPlus_Click(object sender, RoutedEventArgs e)
        {
            if (cboSanPham.SelectedItem == null) return;

            if (int.TryParse(txtSoLuong.Text, out int qty))
            {
                if (rbXuat.IsChecked == true)
                {
                    int nextTotal = qty + 1;
                    if (nextTotal > _totalAvailableStock)
                    {
                        MessageBox.Show($"Không đủ hàng! Kho chỉ còn {_totalAvailableStock}");
                        return;
                    }
                }
                txtSoLuong.Text = (qty + 1).ToString();
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!dgChiTiet.IsMouseOver) dgChiTiet.UnselectAll();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        }
    }
}