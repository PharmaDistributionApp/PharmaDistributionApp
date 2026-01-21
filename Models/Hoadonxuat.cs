namespace PharmaDistributionApp.Models
{
    public partial class Hoadonxuat
    {
        public string Sohdxuat { get; set; } = null!;
        public string? Ngaylap { get; set; }
        public double? Tongtien { get; set; }
        public long? Vat { get; set; }
        public string? Manv { get; set; }
        public string? Makh { get; set; }
        public string? Trangthai { get; set; }

        public string? Ghichu { get; set; }

        public virtual Khachhang? MakhNavigation { get; set; }
        public virtual Nhanvien? ManvNavigation { get; set; }
    }
}