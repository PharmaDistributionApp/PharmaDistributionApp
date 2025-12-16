using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Nhanvien
{
    public string Manv { get; set; } = null!;

    public string Tennv { get; set; } = null!;

    public string? Sdt { get; set; }

    public string? Email { get; set; }

    public string? Diachi { get; set; }

    public DateOnly? Ngaysinh { get; set; }

    public string? Chucvu { get; set; }
}
