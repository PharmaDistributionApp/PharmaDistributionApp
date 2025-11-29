using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Kho
{
    public string Makho { get; set; } = null!;

    public string Tenkho { get; set; } = null!;

    public string? Diachi { get; set; }

    public virtual ICollection<Tonkho> Tonkhos { get; set; } = new List<Tonkho>();
}
