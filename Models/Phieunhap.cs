using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Phieunhap
{
    public string Mapn { get; set; } = null!;

    public string? Sohdnhap { get; set; }

    public string? Makho { get; set; }

    public string? Manv { get; set; }

    public DateTime? Ngaynhap { get; set; }

    public string? Ghichu { get; set; }

    public virtual Kho? MakhoNavigation { get; set; }

    public virtual Nhanvien? ManvNavigation { get; set; }

    public virtual Hoadonnhap? SohdnhapNavigation { get; set; }
}
