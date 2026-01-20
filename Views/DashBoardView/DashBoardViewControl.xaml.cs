using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PharmaDistributionApp.Views.DashBoardView
{
    // Model cho tồn kho (Giữ nguyên)
    public class StockItemModel
    {
        public string TenSP { get; set; }
        public int SoLuong { get; set; }
        public string GiaTriFmt { get; set; }
        public SolidColorBrush ColorCode { get; set; }
    }

    // Model MỚI cho Nhật ký hoạt động
    public class ActivityLogModel
    {
        public DateTime ThoiGian { get; set; }
        public string NguoiThucHien { get; set; }
        public string HanhDong { get; set; }
        public string LoaiHoatDong { get; set; } // 'NHẬP', 'XUẤT', 'HỦY'

        // Màu sắc hiển thị badge
        public SolidColorBrush MauNen { get; set; }
        public SolidColorBrush MauChu { get; set; }
    }

    public partial class DashBoardViewControl : UserControl
    {
        public DashBoardViewControl()
        {
            InitializeComponent();
            txtDate.Text = "Hôm nay: " + DateTime.Now.ToString("dd/MM/yyyy");

            LoadDashboardData();
            LoadStockInventory();
            LoadRecentActivities(); // <--- Gọi hàm này
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
            LoadStockInventory();
            LoadRecentActivities();
        }

        private void LoadAllData()
        {
            LoadDashboardData();
            LoadStockInventory();
            LoadRecentActivities(); // Hàm mới thay cho LoadRecentOrders
        }

        // --- HÀM 1: Load số liệu tổng quan (GIỮ NGUYÊN) ---
        private void LoadDashboardData()
        {
            try
            {
                // Tổng doanh thu
                string sqlRevenue = "SELECT SUM(TONGTIEN) FROM HOADONXUAT";
                object revenueObj = Database.ExecuteScalar(sqlRevenue);
                double revenue = (revenueObj != null && revenueObj != DBNull.Value) ? Convert.ToDouble(revenueObj) : 0;
                txtRevenue.Text = revenue.ToString("#,##0") + " đ";

                // Chi phí nhập
                string sqlExpense = "SELECT SUM(TONGTIEN) FROM HOADONNHAP";
                object expenseObj = Database.ExecuteScalar(sqlExpense);
                double expense = (expenseObj != null && expenseObj != DBNull.Value) ? Convert.ToDouble(expenseObj) : 0;
                txtExpense.Text = expense.ToString("#,##0") + " đ";

                // Sắp hết hàng
                string sqlLowStock = "SELECT COUNT(*) FROM TONKHO WHERE SOLUONGTON < 20";
                object lowStockObj = Database.ExecuteScalar(sqlLowStock);
                txtLowStock.Text = lowStockObj != null ? lowStockObj.ToString() : "0";

                // Khách hàng
                string sqlCustomer = "SELECT COUNT(*) FROM KHACHHANG";
                object custObj = Database.ExecuteScalar(sqlCustomer);
                txtCustomerCount.Text = custObj != null ? custObj.ToString() : "0";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Dashboard Error: " + ex.Message);
            }
        }

        // --- HÀM 2 (MỚI): Load Hoạt động gần đây (Truy vết) ---
        private void LoadRecentActivities()
        {
            try
            {
                // Câu lệnh SQL sử dụng UNION để gộp dữ liệu Nhập và Xuất
                // Logic:
                // - Nếu Trạng thái là 'Đã hủy' -> Coi là thao tác XÓA/HỦY
                // - Hóa đơn Xuất -> Coi là thao tác XUẤT KHO (Bán hàng)
                // - Hóa đơn Nhập -> Coi là thao tác NHẬP KHO

                string sql = @"
                    SELECT * FROM (
                        -- 1. Lấy dữ liệu Hóa Đơn Xuất
                        SELECT 
                            NGAYLAP as ThoiGian, 
                            IFNULL(MANV, 'NV???') as NguoiThucHien,
                            CASE 
                                WHEN TRANGTHAI = 'Đã hủy' THEN 'Đã hủy đơn xuất ' || SOHDXUAT
                                ELSE 'Xuất kho đơn ' || SOHDXUAT || ' (' || printf('%,d', TONGTIEN) || ' đ)'
                            END as HanhDong,
                            CASE 
                                WHEN TRANGTHAI = 'Đã hủy' THEN 'HỦY BỎ'
                                ELSE 'XUẤT KHO'
                            END as Loai
                        FROM HOADONXUAT

                        UNION ALL

                        -- 2. Lấy dữ liệu Hóa Đơn Nhập
                        SELECT 
                            NGAYLAP as ThoiGian, 
                            IFNULL(MANV, 'NV???') as NguoiThucHien,
                            CASE 
                                WHEN TRANGTHAI = 'Đã hủy' THEN 'Đã hủy đơn nhập ' || SOHDNHAP
                                ELSE 'Nhập kho đơn ' || SOHDNHAP || ' (' || printf('%,d', TONGTIEN) || ' đ)'
                            END as HanhDong,
                            CASE 
                                WHEN TRANGTHAI = 'Đã hủy' THEN 'HỦY BỎ'
                                ELSE 'NHẬP KHO'
                            END as Loai
                        FROM HOADONNHAP
                    ) 
                    ORDER BY ThoiGian DESC 
                    LIMIT 20"; // Lấy 20 hoạt động mới nhất

                DataTable dt = Database.GetTable(sql);
                var activities = new List<ActivityLogModel>();

                foreach (DataRow row in dt.Rows)
                {
                    string loai = row["Loai"].ToString();

                    // Cấu hình màu sắc Badge (Tag)
                    string bgCode = "#EEEEEE";
                    string fgCode = "#333333";

                    switch (loai)
                    {
                        case "XUẤT KHO":
                            bgCode = "#E3F2FD"; fgCode = "#1976D2"; break; // Xanh dương
                        case "NHẬP KHO":
                            bgCode = "#E8F5E9"; fgCode = "#2E7D32"; break; // Xanh lá
                        case "HỦY BỎ":
                            bgCode = "#FFEBEE"; fgCode = "#C62828"; break; // Đỏ
                    }

                    activities.Add(new ActivityLogModel
                    {
                        ThoiGian = Convert.ToDateTime(row["ThoiGian"]),
                        NguoiThucHien = row["NguoiThucHien"].ToString(),
                        HanhDong = row["HanhDong"].ToString(),
                        LoaiHoatDong = loai,
                        MauNen = (SolidColorBrush)new BrushConverter().ConvertFrom(bgCode),
                        MauChu = (SolidColorBrush)new BrushConverter().ConvertFrom(fgCode)
                    });
                }

                // Gán vào DataGrid (dgActivities là tên DataGrid trong file XAML)
                dgActivities.ItemsSource = activities;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi ra cửa sổ Output để debug nếu cần
                System.Diagnostics.Debug.WriteLine("Activity Log Error: " + ex.Message);
            }
        }

        // --- HÀM 3: Load tồn kho (GIỮ NGUYÊN) ---
        private void LoadStockInventory()
        {
            try
            {
                string sql = @"
                    SELECT S.TENSP, T.SOLUONGTON, S.GIABAN, (T.SOLUONGTON * S.GIABAN) AS TONG_GIA_TRI
                    FROM TONKHO T
                    JOIN SANPHAM S ON T.MASP = S.MASP
                    WHERE T.SOLUONGTON > 0
                    ORDER BY TONG_GIA_TRI DESC
                    LIMIT 7";

                DataTable dt = Database.GetTable(sql);

                var list = new List<StockItemModel>();
                var colors = new List<string> { "#FF5722", "#2196F3", "#4CAF50", "#FFC107", "#9C27B0", "#00BCD4", "#795548" };
                int colorIndex = 0;

                foreach (DataRow row in dt.Rows)
                {
                    double val = Convert.ToDouble(row["TONG_GIA_TRI"]);
                    list.Add(new StockItemModel
                    {
                        TenSP = row["TENSP"].ToString(),
                        SoLuong = Convert.ToInt32(row["SOLUONGTON"]),
                        GiaTriFmt = val.ToString("#,##0"),
                        ColorCode = (SolidColorBrush)new BrushConverter().ConvertFrom(colors[colorIndex % colors.Count])
                    });
                    colorIndex++;
                }
                icStockList.ItemsSource = list;

                // Tính tổng toàn kho
                string sqlTotal = "SELECT SUM(T.SOLUONGTON * S.GIABAN) FROM TONKHO T JOIN SANPHAM S ON T.MASP = S.MASP";
                object objTotal = Database.ExecuteScalar(sqlTotal);
                double grandTotal = (objTotal != null && objTotal != DBNull.Value) ? Convert.ToDouble(objTotal) : 0;
                txtTotalStockValue.Text = grandTotal.ToString("#,##0") + " đ";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Inventory Error: " + ex.Message);
            }
        }
    }
}