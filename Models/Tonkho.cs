using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Tonkho
{
    public string Makho { get; set; } = null!;

    public string Masp { get; set; } = null!;

    public string Malo { get; set; } = null!;

    public int? Soluongton { get; set; }

    public virtual Kho MakhoNavigation { get; set; } = null!;

    public virtual Lohang MaloNavigation { get; set; } = null!;

    public virtual Sanpham MaspNavigation { get; set; } = null!;
}
