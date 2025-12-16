public partial class Taikhoan
{
    public string Manv { get; set; } = null!; // Khóa chính
    public string Matkhau { get; set; } = null!;
    public string? Quyenhan { get; set; }
    public int? Trangthai { get; set; }
}