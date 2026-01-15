using System;
using System.Data;
using Microsoft.Data.SqlClient; // Thư viện quan trọng nhất

namespace PharmaDistributionApp.Services
{
    public class Database
    {
        // Chuỗi kết nối SQL Server
        private static readonly string _connectionString =
            "Server=.\\SQLEXPRESS;Database=PharmaDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // 1. Hàm lấy kết nối
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        // 2. Hàm lấy dữ liệu dạng Bảng (Dùng cho SELECT: Đăng nhập, Tìm kiếm...)
        public static DataTable GetTable(string sql, SqlParameter[] parameters = null)
        {
            DataTable dt = new DataTable();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (parameters != null)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }

                        using (var adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi lấy dữ liệu: " + ex.Message);
            }
            return dt;
        }

        // 3. Hàm thực thi lệnh (Dùng cho INSERT, UPDATE, DELETE)
        // Trả về số dòng bị ảnh hưởng (int) để biết thành công hay thất bại
        public static int ExecuteNonQuery(string sql, SqlParameter[] parameters = null)
        {
            int rowsAffected = 0;
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (parameters != null)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }

                        // Lệnh này trả về số dòng tác động
                        rowsAffected = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi thực thi lệnh: " + ex.Message);
            }
            return rowsAffected;
        }

        // Thêm vào file Database.cs (class Database)

        // 4. Hàm thực thi và trả về 1 giá trị duy nhất (Dùng cho COUNT, SUM...)
        public static object ExecuteScalar(string sql, SqlParameter[] parameters = null)
        {
            object result = null;
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        if (parameters != null)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }

                        // Trả về giá trị ô đầu tiên của dòng đầu tiên
                        result = cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi ExecuteScalar: " + ex.Message);
            }
            return result;
        }
    }
}