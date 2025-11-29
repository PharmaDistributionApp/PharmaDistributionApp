using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Lohang
{
    public string Malo { get; set; } = null!;

    public string Masp { get; set; } = null!;

    public string? Sohieu { get; set; }

    public DateOnly? Nsx { get; set; }

    public DateOnly? Hsd { get; set; }

    public virtual ICollection<Cthdnhap> Cthdnhaps { get; set; } = new List<Cthdnhap>();

    public virtual ICollection<Cthdxuat> Cthdxuats { get; set; } = new List<Cthdxuat>();

    public virtual Sanpham MaspNavigation { get; set; } = null!;

    public virtual ICollection<Tonkho> Tonkhos { get; set; } = new List<Tonkho>();
}
