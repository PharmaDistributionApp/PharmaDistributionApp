public partial class Nhanvien
{
    public string Manv { get; set; } = null!;
    public string Tennv { get; set; } = null!;
    public string? Chucvu { get; set; }
    public string? Email { get; set; }
    public string? Sdt { get; set; }
    public string? Diachi { get; set; }

    // --- THÊM DÒNG NÀY ---
    public DateOnly? Ngaysinh { get; set; }

    public byte[]? Avatar { get; set; }
}