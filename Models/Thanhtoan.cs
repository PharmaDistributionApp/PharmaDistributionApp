using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Thanhtoan
{
    public string Matt { get; set; } = null!;

    public string? Sohdxuat { get; set; }

    public decimal? Sotien { get; set; }

    public DateTime? Ngaythanhtoan { get; set; }

    public string? Phuongthuc { get; set; }

    public string? Ghichu { get; set; }

    public string? Manv { get; set; }

    public virtual Nhanvien? ManvNavigation { get; set; }

    public virtual Hoadonxuat? SohdxuatNavigation { get; set; }
}
