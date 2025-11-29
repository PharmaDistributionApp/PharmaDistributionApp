using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Nhacungcap
{
    public string Mancc { get; set; } = null!;

    public string Tenncc { get; set; } = null!;

    public string? Sdt { get; set; }

    public string? Email { get; set; }

    public string? Diachi { get; set; }

    public string? Masothue { get; set; }

    public virtual ICollection<Hdnhap> Hdnhaps { get; set; } = new List<Hdnhap>();
}
