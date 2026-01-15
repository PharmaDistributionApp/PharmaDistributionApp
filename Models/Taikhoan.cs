using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Taikhoan
{
    public string Manv { get; set; } = null!;

    public string? Matkhau { get; set; }

    public string? Quyenhan { get; set; }

    public int? Trangthai { get; set; }

    public virtual Nhanvien ManvNavigation { get; set; } = null!;
}
