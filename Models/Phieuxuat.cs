using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Phieuxuat
{
    public string Mapx { get; set; } = null!;

    public string? Sohdxuat { get; set; }

    public string? Makho { get; set; }

    public string? Manv { get; set; }

    public DateTime? Ngayxuat { get; set; }

    public string? Lydo { get; set; }

    public virtual Kho? MakhoNavigation { get; set; }

    public virtual Nhanvien? ManvNavigation { get; set; }

    public virtual Hoadonxuat? SohdxuatNavigation { get; set; }
}
