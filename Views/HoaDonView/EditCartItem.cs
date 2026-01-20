using System;

namespace PharmaDistributionApp.Models
{
    public class EditCartItem
    {
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string MaLo { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
        public string DonVi { get; set; }
        public string DonGiaStr => string.Format("{0:N0}", DonGia);
        public string ThanhTienStr => string.Format("{0:N0}", ThanhTien);
    }
}