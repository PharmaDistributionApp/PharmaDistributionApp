using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Windows;
using PharmaDistributionApp.Models;
using PharmaDistributionApp.Services;

namespace PharmaDistributionApp.Views
{
    public partial class EditInvoiceWindow : Window
    {
        private InvoiceViewModel _invoice;
        private string _tableName;
        private string _colIdName;
        private string _colPartnerId;
        public class PartnerItem { public string Ma { get; set; } public string Ten { get; set; } }

        public EditInvoiceWindow(InvoiceViewModel invoice)
        {
            InitializeComponent();
            _invoice = invoice;
            if (_invoice.LoaiHD == "Xuất") { _tableName = "HOADONXUAT"; _colIdName = "SOHDXUAT"; _colPartnerId = "MAKH"; LoadPartners("KHACHHANG", "MAKH", "TENKH"); }
            else { _tableName = "HOADONNHAP"; _colIdName = "SOHDNHAP"; _colPartnerId = "MANCC"; LoadPartners("NHACUNGCAP", "MANCC", "TENNCC"); }
            LoadCurrentData();
        }

        private void LoadPartners(string table, string colId, string colName)
        {
            try
            {
                var dt = Database.GetTable($"SELECT {colId}, {colName} FROM {table}");
                var list = new List<PartnerItem>();
                foreach (DataRow r in dt.Rows) list.Add(new PartnerItem { Ma = r[0].ToString(), Ten = r[1].ToString() });
                cbbDoiTac.ItemsSource = list;
            }
            catch { }
        }

        private void LoadCurrentData()
        {
            txtMaHD.Text = _invoice.MaHD;
            dpNgayLap.SelectedDate = _invoice.NgayLap;
            try
            {
                var dt = Database.GetTable($"SELECT {_colPartnerId}, GHICHU FROM {_tableName} WHERE {_colIdName} = '{_invoice.MaHD}'");
                if (dt.Rows.Count > 0)
                {
                    cbbDoiTac.SelectedValue = dt.Rows[0][0].ToString();
                    txtGhiChu.Text = dt.Rows[0]["GHICHU"].ToString();
                }
            }
            catch { }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string newPartner = cbbDoiTac.SelectedValue.ToString();
                string newDate = dpNgayLap.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
                string newNote = txtGhiChu.Text.Trim();
                using (var conn = new SQLiteConnection("Data Source=PharmaDB.db"))
                {
                    conn.Open();
                    new SQLiteCommand($"UPDATE {_tableName} SET {_colPartnerId}='{newPartner}', NGAYLAP='{newDate}', GHICHU='{newNote}', TRANGTHAI='Chờ duyệt' WHERE {_colIdName}='{_invoice.MaHD}'", conn).ExecuteNonQuery();
                }
                MessageBox.Show("Đã lưu và gửi duyệt!");
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}