using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Hoadonxuat
{
    public string Sohdxuat { get; set; } = null!;

    public DateTime? Ngaylap { get; set; }

    public decimal? Tongtien { get; set; }

    public double? Vat { get; set; }

    public string? Manv { get; set; }

    public string? Makh { get; set; }

    public string? Trangthai { get; set; }

    public int? TrangthaiDuyet { get; set; }

    public virtual ICollection<Cthdxuat> Cthdxuats { get; set; } = new List<Cthdxuat>();

    public virtual Khachhang? MakhNavigation { get; set; }

    public virtual Nhanvien? ManvNavigation { get; set; }

    public virtual ICollection<Phieuxuat> Phieuxuats { get; set; } = new List<Phieuxuat>();

    public virtual ICollection<Thanhtoan> Thanhtoans { get; set; } = new List<Thanhtoan>();
}
