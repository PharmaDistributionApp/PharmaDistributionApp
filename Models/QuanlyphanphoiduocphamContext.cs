using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PharmaDistributionApp.Models;

public partial class QuanlyphanphoiduocphamContext : DbContext
{
    public QuanlyphanphoiduocphamContext() { }
    public QuanlyphanphoiduocphamContext(DbContextOptions<QuanlyphanphoiduocphamContext> options) : base(options) { }

    // --- KHAI BÁO CÁC BẢNG (Dùng tên Class mới của bạn) ---
    public virtual DbSet<Hoadonnhap> Hoadonnhaps { get; set; } // Class Hoadonnhap
    public virtual DbSet<Hoadonxuat> Hoadonxuats { get; set; } // Class Hoadonxuat
    public virtual DbSet<Phieunhap> Phieunhaps { get; set; }   // Class Phieunhap
    public virtual DbSet<Phieuxuat> Phieuxuats { get; set; }   // Class Phieuxuat

    public virtual DbSet<Cthdnhap> Cthdnhaps { get; set; }
    public virtual DbSet<Cthdxuat> Cthdxuats { get; set; }
    public virtual DbSet<Khachhang> Khachhangs { get; set; }
    public virtual DbSet<Kho> Khos { get; set; }
    public virtual DbSet<Loaisp> Loaisps { get; set; }
    public virtual DbSet<Lohang> Lohangs { get; set; }
    public virtual DbSet<Nhacungcap> Nhacungcaps { get; set; }
    public virtual DbSet<Nhanvien> Nhanviens { get; set; }
    public virtual DbSet<Sanpham> Sanphams { get; set; }
    public virtual DbSet<Taikhoan> Taikhoans { get; set; }
    public virtual DbSet<Tonkho> Tonkhos { get; set; }
    public virtual DbSet<Thanhtoan> Thanhtoans { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite("Data Source=PharmaDB.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. BẢNG HÓA ĐƠN NHẬP (Class Hoadonnhap -> Bảng HOADONNHAP)
        modelBuilder.Entity<Hoadonnhap>(entity =>
        {
            entity.ToTable("HOADONNHAP");
            entity.HasKey(e => e.Sohdnhap);
            entity.Property(e => e.Sohdnhap).HasColumnName("SOHDNHAP");
            entity.Property(e => e.Ngaylap).HasColumnName("NGAYLAP");
            entity.Property(e => e.Tongtien).HasColumnName("TONGTIEN");
            entity.Property(e => e.Manv).HasColumnName("MANV");
            entity.Property(e => e.Mancc).HasColumnName("MANCC");
            entity.Property(e => e.Ghichu).HasColumnName("GHICHU");
        });

        // 2. BẢNG HÓA ĐƠN XUẤT (Class Hoadonxuat -> Bảng HOADONXUAT)
        modelBuilder.Entity<Hoadonxuat>(entity =>
        {
            entity.ToTable("HOADONXUAT");
            entity.HasKey(e => e.Sohdxuat);
            entity.Property(e => e.Sohdxuat).HasColumnName("SOHDXUAT");
            entity.Property(e => e.Ngaylap).HasColumnName("NGAYLAP");
            entity.Property(e => e.Tongtien).HasColumnName("TONGTIEN");
            entity.Property(e => e.Vat).HasColumnName("VAT");
            entity.Property(e => e.Manv).HasColumnName("MANV");
            entity.Property(e => e.Makh).HasColumnName("MAKH");
        });

        // 3. BẢNG PHIẾU NHẬP KHO (Class Phieunhap -> Bảng PHIEUNHAP)
        modelBuilder.Entity<Phieunhap>(entity =>
        {
            entity.ToTable("PHIEUNHAP");
            entity.HasKey(e => e.Mapn);
            entity.Property(e => e.Mapn).HasColumnName("MAPN");
            entity.Property(e => e.Sohdnhap).HasColumnName("SOHDNHAP");
            entity.Property(e => e.Makho).HasColumnName("MAKHO");
            entity.Property(e => e.Manv).HasColumnName("MANV");
            entity.Property(e => e.Ngaynhap).HasColumnName("NGAYNHAP");
            entity.Property(e => e.Ghichu).HasColumnName("GHICHU");
        });

        // 4. BẢNG PHIẾU XUẤT KHO (Class Phieuxuat -> Bảng PHIEUXUAT)
        modelBuilder.Entity<Phieuxuat>(entity =>
        {
            entity.ToTable("PHIEUXUAT");
            entity.HasKey(e => e.Mapx);
            entity.Property(e => e.Mapx).HasColumnName("MAPX");
            entity.Property(e => e.Sohdxuat).HasColumnName("SOHDXUAT");
            entity.Property(e => e.Makho).HasColumnName("MAKHO");
            entity.Property(e => e.Manv).HasColumnName("MANV");
            entity.Property(e => e.Ngayxuat).HasColumnName("NGAYXUAT");
            entity.Property(e => e.Lydo).HasColumnName("LYDO");
        });

        // 5. CÁC BẢNG KHÁC (Giữ nguyên)
        modelBuilder.Entity<Taikhoan>(entity => { entity.ToTable("TAIKHOAN"); entity.HasKey(e => e.Tentk); });
        modelBuilder.Entity<Nhanvien>(entity => { entity.ToTable("NHANVIEN"); entity.HasKey(e => e.Manv); });
        modelBuilder.Entity<Sanpham>(entity => { entity.ToTable("SANPHAM"); entity.HasKey(e => e.Masp); });
        modelBuilder.Entity<Lohang>(entity => { entity.ToTable("LOHANG"); entity.HasKey(e => e.Malo); });
        modelBuilder.Entity<Tonkho>(entity => { entity.ToTable("TONKHO"); entity.HasKey(e => new { e.Makho, e.Masp, e.Malo }); });

        // Cấu hình thêm cho các bảng còn lại nếu cần (Khachhang, Nhacungcap...) giống mẫu trên
        modelBuilder.Entity<Cthdnhap>(entity =>
        {
            entity.ToTable("CTHDNHAP");
            // Quan trọng: Định nghĩa khóa chính tổ hợp
            entity.HasKey(e => new { e.Sohdnhap, e.Masp, e.Malo });

            entity.Property(e => e.Sohdnhap).HasColumnName("SOHDNHAP");
            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasColumnName("MALO");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Dongianhap).HasColumnName("DONGIANHAP");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");
        });

        // 2. Cấu hình Khóa chính cho CHI TIẾT XUẤT (Gồm 3 cột)
        modelBuilder.Entity<Cthdxuat>(entity =>
        {
            entity.ToTable("CTHDXUAT");
            // Quan trọng: Định nghĩa khóa chính tổ hợp
            entity.HasKey(e => new { e.Sohdxuat, e.Masp, e.Malo });

            entity.Property(e => e.Sohdxuat).HasColumnName("SOHDXUAT");
            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasColumnName("MALO");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Dongiaban).HasColumnName("DONGIABAN");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");
        });

        modelBuilder.Entity<Taikhoan>(entity =>
        {
            entity.ToTable("TAIKHOAN");

            // Cấu hình MANV là Khóa Chính
            entity.HasKey(e => e.Manv);

            entity.Property(e => e.Manv).HasColumnName("MANV");
            entity.Property(e => e.Tentk).HasColumnName("TENTK"); // Map cột TENTK
            entity.Property(e => e.Matkhau).HasColumnName("MATKHAU");
            entity.Property(e => e.Quyenhan).HasColumnName("QUYENHAN");
            entity.Property(e => e.Trangthai).HasColumnName("TRANGTHAI");
        });

        // 2. NHÂN VIÊN
        modelBuilder.Entity<Nhanvien>(entity =>
        {
            entity.ToTable("NHANVIEN");
            entity.HasKey(e => e.Manv);
            entity.Property(e => e.Manv).HasColumnName("MANV");
            entity.Property(e => e.Tennv).HasColumnName("TENNV");
            entity.Property(e => e.Chucvu).HasColumnName("CHUCVU");
            entity.Property(e => e.Email).HasColumnName("EMAIL");
            entity.Property(e => e.Sdt).HasColumnName("SDT");
            entity.Property(e => e.Diachi).HasColumnName("DIACHI");
        });

        // 3. KHÁCH HÀNG (Bạn đang bị lỗi cái này)
        modelBuilder.Entity<Khachhang>(entity =>
        {
            entity.ToTable("KHACHHANG");
            entity.HasKey(e => e.Makh);
            entity.Property(e => e.Makh).HasColumnName("MAKH");
            entity.Property(e => e.Tenkh).HasColumnName("TENKH");
            entity.Property(e => e.Sdt).HasColumnName("SDT");
            entity.Property(e => e.Diachi).HasColumnName("DIACHI");
            entity.Property(e => e.Loaikh).HasColumnName("LOAIKH");
        });

        // 4. NHÀ CUNG CẤP
        modelBuilder.Entity<Nhacungcap>(entity =>
        {
            entity.ToTable("NHACUNGCAP");
            entity.HasKey(e => e.Mancc);
            entity.Property(e => e.Mancc).HasColumnName("MANCC");
            entity.Property(e => e.Tenncc).HasColumnName("TENNCC");
            entity.Property(e => e.Sdt).HasColumnName("SDT");
            entity.Property(e => e.Email).HasColumnName("EMAIL");
            entity.Property(e => e.Diachi).HasColumnName("DIACHI");
        });

        // 5. KHO
        modelBuilder.Entity<Kho>(entity =>
        {
            entity.ToTable("KHO");
            entity.HasKey(e => e.Makho);
            entity.Property(e => e.Makho).HasColumnName("MAKHO");
            entity.Property(e => e.Tenkho).HasColumnName("TENKHO");
            entity.Property(e => e.Diachi).HasColumnName("DIACHI");
        });

        // 6. LOẠI SẢN PHẨM
        modelBuilder.Entity<Loaisp>(entity =>
        {
            entity.ToTable("LOAISP");
            entity.HasKey(e => e.Maloai);
            entity.Property(e => e.Maloai).HasColumnName("MALOAI");
            entity.Property(e => e.Tenloai).HasColumnName("TENLOAI");
        });

        // 7. SẢN PHẨM
        modelBuilder.Entity<Sanpham>(entity =>
        {
            entity.ToTable("SANPHAM");
            entity.HasKey(e => e.Masp);
            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Tensp).HasColumnName("TENSP");
            entity.Property(e => e.Dvt).HasColumnName("DVT");
            entity.Property(e => e.Giaban).HasColumnName("GIABAN");
            entity.Property(e => e.Hoatchat).HasColumnName("HOATCHAT");
            entity.Property(e => e.Nuocsx).HasColumnName("NUOCSX");
            entity.Property(e => e.Maloai).HasColumnName("MALOAI");
        });

        // 8. LÔ HÀNG
        modelBuilder.Entity<Lohang>(entity =>
        {
            entity.ToTable("LOHANG");
            entity.HasKey(e => e.Malo);
            entity.Property(e => e.Malo).HasColumnName("MALO");
            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Sohieu).HasColumnName("SOHIEU");
            entity.Property(e => e.Nsx).HasColumnName("NSX");
            entity.Property(e => e.Hsd).HasColumnName("HSD");
        });

        // 9. TỒN KHO (Khóa tổ hợp 3 cột)
        modelBuilder.Entity<Tonkho>(entity =>
        {
            entity.ToTable("TONKHO");
            entity.HasKey(e => new { e.Makho, e.Masp, e.Malo });
            entity.Property(e => e.Makho).HasColumnName("MAKHO");
            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasColumnName("MALO");
            entity.Property(e => e.Soluongton).HasColumnName("SOLUONGTON");
        });

        // 10. THANH TOÁN
        modelBuilder.Entity<Thanhtoan>(entity =>
        {
            entity.ToTable("THANHTOAN");
            entity.HasKey(e => e.Matt);
            entity.Property(e => e.Matt).HasColumnName("MATT");
            entity.Property(e => e.Sohdxuat).HasColumnName("SOHDXUAT");
            entity.Property(e => e.Sotien).HasColumnName("SOTIEN");
            entity.Property(e => e.Ngaythanhtoan).HasColumnName("NGAYTHANHTOAN");
            entity.Property(e => e.Phuongthuc).HasColumnName("PHUONGTHUC");
            entity.Property(e => e.Ghichu).HasColumnName("GHICHU");
            entity.Property(e => e.Manv).HasColumnName("MANV");
        });
    }
}