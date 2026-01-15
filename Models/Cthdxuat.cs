using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Cthdxuat
{
    public string Sohdxuat { get; set; } = null!;

    public string Masp { get; set; } = null!;

    public string Malo { get; set; } = null!;

    public int? Soluong { get; set; }

    public decimal? Dongiaban { get; set; }

    public decimal? Thanhtien { get; set; }

    public virtual Lohang MaloNavigation { get; set; } = null!;

    public virtual Sanpham MaspNavigation { get; set; } = null!;

    public virtual Hoadonxuat SohdxuatNavigation { get; set; } = null!;
}
