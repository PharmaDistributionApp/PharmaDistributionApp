namespace PharmaDistributionApp.Models
{
    public class InvoiceTempItem
    {
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string MaLo { get; set; }  // Mã nội bộ (sinh tự động)
        public string SoHieu { get; set; } // Số hiệu in trên vỏ hộp (VD: LOT-A123)
        public string DonVi { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }

        // --- THÊM MỚI ---
        public string NSX { get; set; }
        public string HSD { get; set; }
        // ----------------

        public decimal ThanhTien => SoLuong * DonGia;
        public string DisplayName => $"{TenSP} - {SoHieu}";
    }
}