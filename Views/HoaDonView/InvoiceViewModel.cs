using System;

namespace PharmaDistributionApp.Models
{
    public class InvoiceViewModel
    {
        public string MaHD { get; set; }
        public string DoiTac { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; }
        public string LoaiHD { get; set; } // "Nhập" hoặc "Xuất"

        // [QUAN TRỌNG] Cột này map với cột PheDuyet trong SQLite
        // 0: Đã duyệt/Bình thường (Hiện lên bảng chính)
        // 1: Chờ duyệt (Hiện lên bảng thông báo của Sếp)
        public int PheDuyet { get; set; }

        // Property phụ để hiển thị tiền đẹp hơn trên giao diện (Binding)
        public string TongTienString => string.Format("{0:N0} VND", TongTien);

        // Property phụ để hiển thị ngày đẹp hơn
        public string NgayLapString => NgayLap.ToString("dd/MM/yyyy");
    }
}