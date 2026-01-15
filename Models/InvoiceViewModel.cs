using System;

namespace PharmaDistributionApp.Models
{
    public class InvoiceViewModel
    {
        public string MaHD { get; set; }
        public string DoiTac { get; set; } // Tên KH hoặc NCC
        public DateTime NgayLap { get; set; }
        public double TongTien { get; set; } // Dùng double để tương thích Slider
        public string TrangThai { get; set; }
        public string LoaiHD { get; set; } // "Nhập" hoặc "Xuất"

        // Property phụ trợ hiển thị (Read-only)
        public string TongTienString => TongTien.ToString("#,##0") + " đ";
        public string NgayLapString => NgayLap.ToString("dd/MM/yyyy");
    }
}