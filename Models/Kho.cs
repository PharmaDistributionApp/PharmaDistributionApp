using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Kho
{
    public string Makho { get; set; } = null!;

    public string? Tenkho { get; set; }

    public string? Diachi { get; set; }

    public virtual ICollection<Phieunhap> Phieunhaps { get; set; } = new List<Phieunhap>();

    public virtual ICollection<Phieuxuat> Phieuxuats { get; set; } = new List<Phieuxuat>();

    public virtual ICollection<Tonkho> Tonkhos { get; set; } = new List<Tonkho>();
}
