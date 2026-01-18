public partial class Nhanvien
{
    public string Manv { get; set; } = null!;
    public string Tennv { get; set; } = null!;
    public string? Gioitinh { get; set; }
    public string? Chucvu { get; set; }
    public string? Cccd { get; set; }
    public string? Email { get; set; }
    public string? Sdt { get; set; }
    public string? Diachi { get; set; }

    // ĐỔI DateOnly? THÀNH DateTime? Ở ĐÂY
    public DateTime? Ngaysinh { get; set; }

    public byte[]? Avatar { get; set; }
}