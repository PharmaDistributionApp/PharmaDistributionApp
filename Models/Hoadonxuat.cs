namespace PharmaDistributionApp.Models;

public partial class Hoadonxuat
{
    public string Sohdxuat { get; set; } = null!;
    public string? Ngaylap { get; set; }
    public double? Tongtien { get; set; }
    public double? Vat { get; set; }
    public string? Manv { get; set; }
    public string? Makh { get; set; }

    // THÊM LẠI DÒNG NÀY
    public string? Trangthai { get; set; }
}