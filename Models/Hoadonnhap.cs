using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Hoadonnhap
{
    public string Sohdnhap { get; set; } = null!;

    public DateOnly Ngaylap { get; set; }

    public decimal Tongtien { get; set; }

    public string? Ghichu { get; set; }

    public string Manv { get; set; } = null!;

    public string Mancc { get; set; } = null!;
}
