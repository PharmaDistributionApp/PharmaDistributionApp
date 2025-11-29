using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Hdxuat
{
    public string Sohdxuat { get; set; } = null!;

    public DateOnly Ngayxuat { get; set; }

    public decimal Tongtien { get; set; }

    public double? Vat { get; set; }

    public string Manv { get; set; } = null!;

    public string Makh { get; set; } = null!;

    public virtual ICollection<Cthdxuat> Cthdxuats { get; set; } = new List<Cthdxuat>();

    public virtual Khachhang MakhNavigation { get; set; } = null!;

    public virtual Nhanvien ManvNavigation { get; set; } = null!;

    public virtual ICollection<Thanhtoan> Thanhtoans { get; set; } = new List<Thanhtoan>();
}
