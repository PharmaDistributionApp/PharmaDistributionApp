using PharmaDistributionApp.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Controls;
using System.Windows.Media; // Dùng cho SolidColorBrush

namespace PharmaDistributionApp.Views.DashBoardView
{
    // 1. Model cho danh sách tồn kho
    public class StockItemModel
    {
        public string TenSP { get; set; }
        public int SoLuong { get; set; }
        public string GiaTriFmt { get; set; }
        public SolidColorBrush ColorCode { get; set; }
    }

    public partial class DashBoardViewControl : UserControl
    {
        public DashBoardViewControl()
        {
            InitializeComponent();

            txtDate.Text = "Hôm nay: " + DateTime.Now.ToString("dd/MM/yyyy");

            LoadDashboardData();
            LoadStockInventory();
        }

        // --- HÀM 1: Load số liệu tổng quan ---
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

                // Load đơn hàng gần đây
                LoadRecentOrders();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Dashboard Error: " + ex.Message);
            }
        }

        // --- HÀM 2: Load danh sách đơn hàng ---
        private void LoadRecentOrders()
        {
            try
            {
                string sql = "SELECT SOHDXUAT, NGAYLAP, TONGTIEN FROM HOADONXUAT ORDER BY NGAYLAP DESC LIMIT 10";
                DataTable dt = Database.GetTable(sql);

                if (!dt.Columns.Contains("TONGTIEN_FMT"))
                    dt.Columns.Add("TONGTIEN_FMT", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    if (row["TONGTIEN"] != DBNull.Value)
                    {
                        double money = Convert.ToDouble(row["TONGTIEN"]);
                        row["TONGTIEN_FMT"] = money.ToString("#,##0") + " đ";
                    }
                }
                dgRecentOrders.ItemsSource = dt.DefaultView;
            }
            catch { }
        }

        // --- HÀM 3: Load tồn kho (Top giá trị) ---
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