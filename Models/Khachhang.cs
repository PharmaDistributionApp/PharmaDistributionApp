using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Khachhang
{
    public string Makh { get; set; } = null!;

    public string? Tenkh { get; set; }

    public string? Sdt { get; set; }

    public string? Diachi { get; set; }

    public string? Loaikh { get; set; }

    public virtual ICollection<Hoadonxuat> Hoadonxuats { get; set; } = new List<Hoadonxuat>();
}
