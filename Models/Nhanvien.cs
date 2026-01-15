using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Nhanvien
{
    public string Manv { get; set; } = null!;

    public string? Tennv { get; set; }

    public string? Gioitinh { get; set; }

    public string? Chucvu { get; set; }

    public string? Email { get; set; }

    public string? Cccd { get; set; }

    public string? Sdt { get; set; }

    public string? Diachi { get; set; }

    public DateOnly? Ngaysinh { get; set; }

    public int? Trangthai { get; set; }

    public byte[]? Avatar { get; set; }

    public virtual ICollection<Hoadonnhap> Hoadonnhaps { get; set; } = new List<Hoadonnhap>();

    public virtual ICollection<Hoadonxuat> Hoadonxuats { get; set; } = new List<Hoadonxuat>();

    public virtual ICollection<Phieunhap> Phieunhaps { get; set; } = new List<Phieunhap>();

    public virtual ICollection<Phieuxuat> Phieuxuats { get; set; } = new List<Phieuxuat>();

    public virtual Taikhoan? Taikhoan { get; set; }

    public virtual ICollection<Thanhtoan> Thanhtoans { get; set; } = new List<Thanhtoan>();
}
