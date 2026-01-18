namespace PharmaDistributionApp.Models
{
    public class ChiTietHoaDonItem
    {
        public int STT { get; set; }
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string MaLo { get; set; }
        public string DonVi { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }

        public decimal ThanhTien => SoLuong * DonGia;

        // [SỬA ĐỔI]: Đổi "đ" thành "VND"
        public string DonGiaStr => string.Format("{0:N0} VND", DonGia);
        public string ThanhTienStr => string.Format("{0:N0} VND", ThanhTien);
    }
}