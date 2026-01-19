using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Lohang
{
    public string Malo { get; set; } = null!;

    public string Masp { get; set; } = null!;

    public DateOnly? Nsx { get; set; }

    public DateOnly? Hsd { get; set; }
    public string? Nhacungcap { get; set; }

}
