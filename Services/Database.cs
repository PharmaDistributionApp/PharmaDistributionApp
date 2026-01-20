using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
// ĐÃ XÓA: using iTextSharp.text.pdf.parser; (Nguyên nhân gây lỗi Ambiguous)

namespace PharmaDistributionApp.Services
{
    public class Database
    {
        // 1. Xác định đường dẫn tuyệt đối đến file DB (Fix lỗi dữ liệu không đồng bộ)
        // Dùng System.IO.Path để tránh nhầm lẫn
        private static readonly string _dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PharmaDB.db");

        // 2. Chuỗi kết nối CHUẨN (Dùng chung cho cả hàm nội bộ và bên ngoài)
        public static string ConnectionString => $"Data Source={_dbPath};Version=3;";

        // 3. Hàm lấy kết nối (Đã sửa để dùng chung ConnectionString chuẩn)
        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(ConnectionString);
        }

        // 4. Hàm lấy bảng dữ liệu (SELECT)
        public static DataTable GetTable(string sql, SQLiteParameter[] parameters = null)
        {
            // Sử dụng GetConnection để đảm bảo đồng bộ
            using (SQLiteConnection conn = GetConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        // 5. Hàm thực thi lệnh (INSERT, UPDATE, DELETE)
        public static int ExecuteNonQuery(string sql, SQLiteParameter[] parameters = null)
        {
            using (SQLiteConnection conn = GetConnection())
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    return cmd.ExecuteNonQuery(); // Trả về số dòng bị ảnh hưởng
                }
            }
        }

        // 6. Hàm lấy giá trị đơn (VD: Lấy tổng tiền, lấy số lượng...)
        public static object ExecuteScalar(string sql, SQLiteParameter[] parameters = null)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    return command.ExecuteScalar();
                }
            }
        }
    }
}