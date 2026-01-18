namespace PharmaDistributionApp.Models
{
    public class InvoiceViewModel
    {
        public string MaHD { get; set; }
        public string DoiTac { get; set; }
        public DateTime NgayLap { get; set; }

        // [SỬA LẠI]: Đổi từ double sang decimal
        public decimal TongTien { get; set; }

        public string TrangThai { get; set; }
        public string LoaiHD { get; set; }

        // Format tiền tệ
        public string TongTienString => string.Format("{0:N0} VND", TongTien);
    }
}