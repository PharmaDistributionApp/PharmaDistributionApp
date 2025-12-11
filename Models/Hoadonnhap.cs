using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Hoadonnhap
{
    public string Sohdnhap { get; set; } = null!;
    public string? Ngaylap { get; set; }
    public double? Tongtien { get; set; }
    public string? Manv { get; set; }
    public string? Mancc { get; set; }
    public string? Ghichu { get; set; }
}