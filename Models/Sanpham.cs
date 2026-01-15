using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Sanpham
{
    public string Masp { get; set; } = null!;

    public string? Tensp { get; set; }

    public string? Dvt { get; set; }

    public decimal? Giaban { get; set; }

    public string? Hoatchat { get; set; }

    public string? Nuocsx { get; set; }

    public string? Maloai { get; set; }

    public string? Nhacungcap { get; set; }

    public string? Ghichu { get; set; }

    public virtual ICollection<Cthdnhap> Cthdnhaps { get; set; } = new List<Cthdnhap>();

    public virtual ICollection<Cthdxuat> Cthdxuats { get; set; } = new List<Cthdxuat>();

    public virtual ICollection<Lohang> Lohangs { get; set; } = new List<Lohang>();

    public virtual Loaisp? MaloaiNavigation { get; set; }

    public virtual ICollection<Tonkho> Tonkhos { get; set; } = new List<Tonkho>();
}
