public partial class Khachhang
{
    public string Makh { get; set; } = null!;
    public string Tenkh { get; set; } = null!;
    public string? Sdt { get; set; }
    public string? Diachi { get; set; }
    public string? Loaikh { get; set; }
    public decimal? Doanhso { get; set; }

    // Thêm dòng này
    public string? Email { get; set; }
}