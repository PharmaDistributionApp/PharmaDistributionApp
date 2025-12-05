using System;
using System.Collections.Generic;

namespace PharmaDistributionApp.Models;

public partial class Taikhoan
{
    public string Manv { get; set; } = null!; // Khóa chính
    public string? Tentk { get; set; }        // Tên đăng nhập
    public string Matkhau { get; set; } = null!;
    public string? Quyenhan { get; set; }
    public int? Trangthai { get; set; }
}