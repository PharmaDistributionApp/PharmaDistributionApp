using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Sanpham
{
    public string Masp { get; set; } = null!;

    public string Tensp { get; set; } = null!;

    public string? Dvt { get; set; }

    public string? Hoatchat { get; set; }

    public string? Nuocsx { get; set; }

    public decimal Giaban { get; set; }

    public int? Trangthai { get; set; }

    public string Maloai { get; set; } = null!;

    public virtual ICollection<Cthdnhap> Cthdnhaps { get; set; } = new List<Cthdnhap>();

    public virtual ICollection<Cthdxuat> Cthdxuats { get; set; } = new List<Cthdxuat>();

    public virtual ICollection<Lohang> Lohangs { get; set; } = new List<Lohang>();

    public virtual ICollection<Tonkho> Tonkhos { get; set; } = new List<Tonkho>();
}
