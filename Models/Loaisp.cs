using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Loaisp
{
    public string Maloai { get; set; } = null!;

    public string? Tenloai { get; set; }

    public virtual ICollection<Sanpham> Sanphams { get; set; } = new List<Sanpham>();
}
