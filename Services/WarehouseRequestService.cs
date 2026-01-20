using System;
using System.Collections.Generic;
// SỬA: Dùng thư viện có sẵn của EF Core, không cài thêm System.Data.SQLite
using Microsoft.Data.Sqlite;

namespace PharmaDistributionApp.Services
{
    public static class WarehouseRequestService
    {
        public static void GuiYeuCauTaoPhieuKho(string maHoaDon, bool isXuatHang, string maDoiTac, string maNV)
        {
            // SỬA: Dùng SqliteConnection (chữ 'l' thường)
            using (var conn = new SqliteConnection(Database.ConnectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string table = isXuatHang ? "PHIEUXUAT" : "PHIEUNHAP";
                        string colID = isXuatHang ? "MAPX" : "MAPN";
                        string colRef = isXuatHang ? "SOHDXUAT" : "SOHDNHAP";
                        string prefix = isXuatHang ? "PX" : "PN";

                        // 1. ĐỒNG BỘ LÔ HÀNG (Tự động tạo lô tạm để có HSD hiển thị)
                        if (!isXuatHang)
                        {
                            string hsdTam = DateTime.Now.AddYears(2).ToString("yyyy-MM-dd");
                            // Thêm dòng này để xử lý NSX luôn cho đủ bộ
                            string nsxTam = DateTime.Now.ToString("yyyy-MM-dd");

                            string sqlSyncLot = @"
                                INSERT OR IGNORE INTO LOHANG (MALO, MASP, NHACUNGCAP, HSD, NSX)
                                SELECT DISTINCT MALO, MASP, @ncc, @hsd, @nsx
                                FROM CTHDNHAP 
                                WHERE SOHDNHAP = @ref AND MALO IS NOT NULL AND MALO <> ''";

                            // SỬA: Dùng SqliteCommand (chữ 'l' thường)
                            var cmdSync = new SqliteCommand(sqlSyncLot, conn, transaction);
                            cmdSync.Parameters.AddWithValue("@ncc", maDoiTac);
                            cmdSync.Parameters.AddWithValue("@hsd", hsdTam);
                            cmdSync.Parameters.AddWithValue("@nsx", nsxTam);
                            cmdSync.Parameters.AddWithValue("@ref", maHoaDon);
                            cmdSync.ExecuteNonQuery();
                        }

                        // 2. KIỂM TRA PHIẾU ĐÃ TỒN TẠI CHƯA
                        string sqlCheck = $"SELECT COUNT(*) FROM {table} WHERE {colRef} = @ref";
                        var cmdCheck = new SqliteCommand(sqlCheck, conn, transaction);
                        cmdCheck.Parameters.AddWithValue("@ref", maHoaDon);
                        long count = (long)cmdCheck.ExecuteScalar();

                        if (count > 0)
                        {
                            string sqlUpdate = $"UPDATE {table} SET Trangthai = 'Cập nhật từ HD' WHERE {colRef} = @ref";
                            var cmdUpdate = new SqliteCommand(sqlUpdate, conn, transaction);
                            cmdUpdate.Parameters.AddWithValue("@ref", maHoaDon);
                            cmdUpdate.ExecuteNonQuery();
                        }
                        else
                        {
                            // 3. TẠO MÃ PHIẾU MỚI
                            int maxNum = 0;
                            string sqlGetIDs = $"SELECT {colID} FROM {table}";
                            using (var cmdIDs = new SqliteCommand(sqlGetIDs, conn, transaction))
                            using (var reader = cmdIDs.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string ma = reader[0].ToString();
                                    if (!string.IsNullOrEmpty(ma) && ma.StartsWith(prefix) && ma.Length <= 6)
                                    {
                                        if (int.TryParse(ma.Substring(2), out int num))
                                        {
                                            if (num > maxNum) maxNum = num;
                                        }
                                    }
                                }
                            }

                            string newID = prefix + (maxNum + 1).ToString("D3");
                            string today = DateTime.Now.ToString("yyyy-MM-dd");
                            string khoMacDinh = "KHO_TONG";

                            string sqlInsert = isXuatHang
                                ? @"INSERT INTO PHIEUXUAT (MAPX, NGAYXUAT, SOHDXUAT, MAKHO, MANV, LYDO, Trangthai) 
                                    VALUES (@id, @date, @ref, @kho, @nv, 'Xuất bán hàng', 'Yêu cầu từ HD')"
                                : @"INSERT INTO PHIEUNHAP (MAPN, NGAYNHAP, SOHDNHAP, MAKHO, MANV, GHICHU, Trangthai) 
                                    VALUES (@id, @date, @ref, @kho, @nv, 'Nhập hàng', 'Yêu cầu từ HD')";

                            var cmdInsert = new SqliteCommand(sqlInsert, conn, transaction);
                            cmdInsert.Parameters.AddWithValue("@id", newID);
                            cmdInsert.Parameters.AddWithValue("@date", today);
                            cmdInsert.Parameters.AddWithValue("@ref", maHoaDon);
                            cmdInsert.Parameters.AddWithValue("@kho", khoMacDinh);
                            cmdInsert.Parameters.AddWithValue("@nv", maNV);
                            cmdInsert.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}