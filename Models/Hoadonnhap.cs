using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Hoadonnhap
{
    public string Sohdnhap { get; set; } = null!;

    public DateTime? Ngaylap { get; set; }

    public decimal? Tongtien { get; set; }

    public string? Manv { get; set; }

    public string? Mancc { get; set; }

    public string? Ghichu { get; set; }

    public string? Trangthai { get; set; }

    public virtual ICollection<Cthdnhap> Cthdnhaps { get; set; } = new List<Cthdnhap>();

    public virtual Nhacungcap? ManccNavigation { get; set; }

    public virtual Nhanvien? ManvNavigation { get; set; }

    public virtual ICollection<Phieunhap> Phieunhaps { get; set; } = new List<Phieunhap>();
}
