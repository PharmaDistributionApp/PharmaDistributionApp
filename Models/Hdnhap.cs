using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Hdnhap
{
    public string Sohdnhap { get; set; } = null!;

    public DateOnly Ngaynhap { get; set; }

    public decimal Tongtien { get; set; }

    public string? Ghichu { get; set; }

    public string Manv { get; set; } = null!;

    public string Mancc { get; set; } = null!;

    public virtual ICollection<Cthdnhap> Cthdnhaps { get; set; } = new List<Cthdnhap>();

    public virtual Nhacungcap ManccNavigation { get; set; } = null!;

    public virtual Nhanvien ManvNavigation { get; set; } = null!;
}
