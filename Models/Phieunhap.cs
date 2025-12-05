using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Phieunhap
{
    public string Mapn { get; set; } = null!;
    public string Sohdnhap { get; set; } = null!;
    public string Makho { get; set; } = null!;
    public string Manv { get; set; } = null!;
    public string? Ngaynhap { get; set; } // SQLite lưu Date dạng string
    public string? Ghichu { get; set; }
}