using System;
using System.Data;
using System.Data.SQLite; // Thư viện bạn đã cài
using System.IO;

namespace PharmaDistributionApp.Services
{
    public class Database
    {
        // |DataDirectory| tự động trỏ vào thư mục bin/Debug khi chạy App
        // Tên file của bạn là PharmaDB.db
        private static string _connectionString = "Data Source=|DataDirectory|\\PharmaDB.db;Version=3;New=False;Compress=True;";

        // 1. Hàm lấy kết nối (Dùng khi cần xử lý phức tạp)
        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        // 2. Hàm lấy bảng dữ liệu (Dùng cho SELECT: Đăng nhập, Hiện danh sách...)
        public static DataTable GetTable(string sql, SQLiteParameter[] parameters = null)
        {
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

        // 3. Hàm thực thi lệnh (Dùng cho INSERT, UPDATE, DELETE)
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
    }
}