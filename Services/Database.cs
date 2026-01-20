using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite; // Thư viện chuẩn

namespace PharmaDistributionApp.Services
{
    public class Database
    {
        // 1. Đường dẫn file DB
        private static readonly string _dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PharmaDB.db");

        // 2. Chuỗi kết nối
        public static string ConnectionString => $"Data Source={_dbPath};";

        // 3. Hàm lấy kết nối
        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        // ==================================================================================
        // SỬA LỖI TẠI ĐÂY: Viết lại hàm GetTable để không dùng dt.Load(reader)
        // Cách này giúp tránh lỗi "ConstraintException: Failed to enable constraints"
        // ==================================================================================
        public static DataTable GetTable(string sql, SqliteParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    using (var reader = cmd.ExecuteReader())
                    {
                        var dt = new DataTable();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string colName = reader.GetName(i);
                            Type colType = reader.GetFieldType(i) ?? typeof(object);
                            string originalName = colName;
                            int count = 1;
                            while (dt.Columns.Contains(colName))
                            {
                                colName = $"{originalName}_{count}";
                                count++;
                            }

                            DataColumn col = new DataColumn(colName, colType);
                            col.AllowDBNull = true;
                            dt.Columns.Add(col);
                        }
                        while (reader.Read())
                        {
                            DataRow row = dt.NewRow();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[i] = reader.GetValue(i);
                            }
                            dt.Rows.Add(row);
                        }

                        return dt;
                    }
                }
            }
        }
        public static int ExecuteNonQuery(string sql, SqliteParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqliteCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);

                    return cmd.ExecuteNonQuery();
                }
            }
        }

        // 6. Hàm lấy giá trị đơn
        public static object ExecuteScalar(string sql, SqliteParameter[] parameters = null)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqliteCommand(sql, connection))
                {
                    if (parameters != null)
                        command.Parameters.AddRange(parameters);

                    return command.ExecuteScalar();
                }
            }
        }
    }
}