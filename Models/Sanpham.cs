using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Sanpham
{
    public string Masp { get; set; } = null!;

    public string Tensp { get; set; } = null!;

    public string? Dvt { get; set; }

    public string? Nuocsx { get; set; }

    public decimal Giaban { get; set; }

    public string Maloai { get; set; } = null!;
    public string? Nhacungcap { get; set; }
    public string? Ghichu { get; set; }

}
