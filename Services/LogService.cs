
using System;
using System.Data.SQLite;

namespace PharmaDistributionApp.Services
{
    public static class LogService
    {
        public static void RecordLog(string manv, string hanhDong, string doiTuong, string moTa)
        {
            try
            {
                using (var conn = new SQLiteConnection("Data Source=PharmaDB.db")) 
                {
                    conn.Open();
                    string sql = "INSERT INTO SYSTEM_LOG (THOIGIAN, MANV, HANHDONG, DOITUONG, MOTA) VALUES (@time, @manv, @act, @obj, @desc)";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@time", DateTime.Now);
                        cmd.Parameters.AddWithValue("@manv", manv ?? "System");
                        cmd.Parameters.AddWithValue("@act", hanhDong);
                        cmd.Parameters.AddWithValue("@obj", doiTuong);
                        cmd.Parameters.AddWithValue("@desc", moTa);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi ghi log: " + ex.Message);
            }
        }
    }
}