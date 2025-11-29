using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Thanhtoan
{
    public string Matt { get; set; } = null!;

    public string Sohdxuat { get; set; } = null!;

    public decimal Sotien { get; set; }

    public DateOnly Ngaythanhtoan { get; set; }

    public string? Phuongthuc { get; set; }

    public string? Ghichu { get; set; }

    public string Manv { get; set; } = null!;

    public virtual Nhanvien ManvNavigation { get; set; } = null!;

    public virtual Hdxuat SohdxuatNavigation { get; set; } = null!;
}
