using System;
using System.Data.SQLite;

namespace PharmaDistributionApp.Services
{
    public static class WarehouseRequestService
    {
        /// <summary>
        /// Gửi yêu cầu tạo phiếu kho (Đã chỉnh sửa đúng theo cấu trúc bảng PHIEUNHAP/PHIEUXUAT)
        /// </summary>
        public static void GuiYeuCauTaoPhieuKho(string maHoaDon, bool isXuatHang, string maDoiTac, string maNV)
        {
            using (var conn = new SQLiteConnection(Database.ConnectionString))
            {
                conn.Open();

                // Xác định tên bảng và cột khóa ngoại
                string table = isXuatHang ? "PHIEUXUAT" : "PHIEUNHAP";
                string colRef = isXuatHang ? "SOHDXUAT" : "SOHDNHAP";
                string prefix = isXuatHang ? "PX" : "PN";

                // 1. Kiểm tra xem đã có phiếu nào cho hóa đơn này chưa
                string sqlCheck = $"SELECT COUNT(*) FROM {table} WHERE {colRef} = @ref";
                var cmdCheck = new SQLiteCommand(sqlCheck, conn);
                cmdCheck.Parameters.AddWithValue("@ref", maHoaDon);

                long count = (long)cmdCheck.ExecuteScalar();

                if (count > 0)
                {
                    // Nếu đã có phiếu -> Cập nhật trạng thái
                    string sqlUpdate = $"UPDATE {table} SET Trangthai = 'Cập nhật từ HD' WHERE {colRef} = @ref";
                    var cmdUpdate = new SQLiteCommand(sqlUpdate, conn);
                    cmdUpdate.Parameters.AddWithValue("@ref", maHoaDon);
                    cmdUpdate.ExecuteNonQuery();
                }
                else
                {
                    // 2. Nếu chưa có -> Tạo phiếu mới
                    string newID = prefix + DateTime.Now.ToString("yyyyMMddHHmmss");
                    string today = DateTime.Now.ToString("yyyy-MM-dd"); // Format chuẩn cho cột TEXT ngày tháng
                    string khoMacDinh = "KHO_TONG";

                    string sqlInsert = "";

                    if (isXuatHang)
                    {
                        // Bảng PHIEUXUAT: Sử dụng cột LYDO (Không có GHICHU)
                        sqlInsert = @"INSERT INTO PHIEUXUAT (MAPX, NGAYXUAT, SOHDXUAT, MAKHO, MANV, LYDO, Trangthai) 
                                      VALUES (@id, @date, @ref, @kho, @nv, 'Xuất bán hàng theo hóa đơn', 'Yêu cầu từ HD')";
                    }
                    else
                    {
                        // Bảng PHIEUNHAP: Sử dụng cột GHICHU
                        sqlInsert = @"INSERT INTO PHIEUNHAP (MAPN, NGAYNHAP, SOHDNHAP, MAKHO, MANV, GHICHU, Trangthai) 
                                      VALUES (@id, @date, @ref, @kho, @nv, 'Nhập hàng theo hóa đơn', 'Yêu cầu từ HD')";
                    }

                    var cmdInsert = new SQLiteCommand(sqlInsert, conn);
                    cmdInsert.Parameters.AddWithValue("@id", newID);
                    cmdInsert.Parameters.AddWithValue("@date", today);
                    cmdInsert.Parameters.AddWithValue("@ref", maHoaDon);
                    cmdInsert.Parameters.AddWithValue("@kho", khoMacDinh);
                    cmdInsert.Parameters.AddWithValue("@nv", maNV);

                    cmdInsert.ExecuteNonQuery();
                }
            }
        }
    }
}