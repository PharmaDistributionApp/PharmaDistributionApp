using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Khachhang
{
    public string Makh { get; set; } = null!;

    public string Tenkh { get; set; } = null!;

    public string? Sdt { get; set; }

    public string? Diachi { get; set; }

    public string? Loaikh { get; set; }

    public string? Masothue { get; set; }

    public decimal? Doanhso { get; set; }

    public DateOnly? Ngdk { get; set; }

    public virtual ICollection<Hdxuat> Hdxuats { get; set; } = new List<Hdxuat>();
}
