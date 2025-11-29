using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Taikhoan
{
    public int Idtk { get; set; }

    public string Tentk { get; set; } = null!;

    public string Matkhau { get; set; } = null!;

    public string Quyenhan { get; set; } = null!;

    public int Trangthai { get; set; }

    public string Manv { get; set; } = null!;

    public virtual Nhanvien ManvNavigation { get; set; } = null!;
}
