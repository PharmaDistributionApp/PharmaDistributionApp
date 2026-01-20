using System;

namespace PharmaDistributionApp.Models
{
    public class InvoiceViewModel
    {
        public string MaHD { get; set; }
        public string MaDT { get; set; }
        public string DoiTac { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; }
        public string LoaiHD { get; set; } 
        public int PheDuyet { get; set; }
        public string TongTienString => string.Format("{0:N0} VND", TongTien);
        public string NgayLapString => NgayLap.ToString("dd/MM/yyyy");
    }
}