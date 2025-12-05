using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Hoadonxuat
{
    public string Sohdxuat { get; set; } = null!;

    public DateOnly Ngaylap { get; set; }

    public decimal Tongtien { get; set; }

    public double? Vat { get; set; }

    public string Manv { get; set; } = null!;

    public string Makh { get; set; } = null!;
}
