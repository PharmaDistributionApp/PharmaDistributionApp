namespace PharmaDistributionApp.Models
{
    public class InvoiceTempItem
    {
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string MaLo { get; set; }  
        public string DonVi { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }

        public string NSX { get; set; }
        public string HSD { get; set; }

        public decimal ThanhTien => SoLuong * DonGia;
    }
}