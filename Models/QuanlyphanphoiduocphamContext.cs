using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PharmaDistributionApp.Models;

public partial class QuanlyphanphoiduocphamContext : DbContext
{
    public QuanlyphanphoiduocphamContext()
    {
    }

    public QuanlyphanphoiduocphamContext(DbContextOptions<QuanlyphanphoiduocphamContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cthdnhap> Cthdnhaps { get; set; }

    public virtual DbSet<Cthdxuat> Cthdxuats { get; set; }

    public virtual DbSet<Hoadonnhap> Hoadonnhaps { get; set; }

    public virtual DbSet<Hoadonxuat> Hoadonxuats { get; set; }

    public virtual DbSet<Khachhang> Khachhangs { get; set; }

    public virtual DbSet<Kho> Khos { get; set; }

    public virtual DbSet<Loaisp> Loaisps { get; set; }

    public virtual DbSet<Lohang> Lohangs { get; set; }

    public virtual DbSet<Nhacungcap> Nhacungcaps { get; set; }

    public virtual DbSet<Nhanvien> Nhanviens { get; set; }

    public virtual DbSet<Phieunhap> Phieunhaps { get; set; }

    public virtual DbSet<Phieuxuat> Phieuxuats { get; set; }

    public virtual DbSet<Sanpham> Sanphams { get; set; }

    public virtual DbSet<Taikhoan> Taikhoans { get; set; }

    public virtual DbSet<Thanhtoan> Thanhtoans { get; set; }

    public virtual DbSet<Tonkho> Tonkhos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=PharmaDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cthdnhap>(entity =>
        {
            entity.HasKey(e => new { e.Sohdnhap, e.Masp, e.Malo }).HasName("PK__CTHDNHAP__03E8DC7FEC7ABD52");

            entity.ToTable("CTHDNHAP");

            entity.Property(e => e.Sohdnhap)
                .HasMaxLength(20)
                .HasColumnName("SOHDNHAP");
            entity.Property(e => e.Masp)
                .HasMaxLength(20)
                .HasColumnName("MASP");
            entity.Property(e => e.Malo)
                .HasMaxLength(20)
                .HasColumnName("MALO");
            entity.Property(e => e.Dongianhap)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("DONGIANHAP");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("THANHTIEN");

            entity.HasOne(d => d.MaloNavigation).WithMany(p => p.Cthdnhaps)
                .HasForeignKey(d => d.Malo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHDNHAP_LO");

            entity.HasOne(d => d.MaspNavigation).WithMany(p => p.Cthdnhaps)
                .HasForeignKey(d => d.Masp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHDNHAP_SP");

            entity.HasOne(d => d.SohdnhapNavigation).WithMany(p => p.Cthdnhaps)
                .HasForeignKey(d => d.Sohdnhap)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHDNHAP_HD");
        });

        modelBuilder.Entity<Cthdxuat>(entity =>
        {
            entity.HasKey(e => new { e.Sohdxuat, e.Masp, e.Malo }).HasName("PK__CTHDXUAT__A37DB43B60333034");

            entity.ToTable("CTHDXUAT");

            entity.Property(e => e.Sohdxuat)
                .HasMaxLength(20)
                .HasColumnName("SOHDXUAT");
            entity.Property(e => e.Masp)
                .HasMaxLength(20)
                .HasColumnName("MASP");
            entity.Property(e => e.Malo)
                .HasMaxLength(20)
                .HasColumnName("MALO");
            entity.Property(e => e.Dongiaban)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("DONGIABAN");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("THANHTIEN");

            entity.HasOne(d => d.MaloNavigation).WithMany(p => p.Cthdxuats)
                .HasForeignKey(d => d.Malo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHDXUAT_LO");

            entity.HasOne(d => d.MaspNavigation).WithMany(p => p.Cthdxuats)
                .HasForeignKey(d => d.Masp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHDXUAT_SP");

            entity.HasOne(d => d.SohdxuatNavigation).WithMany(p => p.Cthdxuats)
                .HasForeignKey(d => d.Sohdxuat)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHDXUAT_HD");
        });

        modelBuilder.Entity<Hoadonnhap>(entity =>
        {
            entity.HasKey(e => e.Sohdnhap).HasName("PK__HOADONNH__608ACB9D3F5D74C9");

            entity.ToTable("HOADONNHAP");

            entity.Property(e => e.Sohdnhap)
                .HasMaxLength(20)
                .HasColumnName("SOHDNHAP");
            entity.Property(e => e.Ghichu).HasColumnName("GHICHU");
            entity.Property(e => e.Mancc)
                .HasMaxLength(20)
                .HasColumnName("MANCC");
            entity.Property(e => e.Manv)
                .HasMaxLength(20)
                .HasColumnName("MANV");
            entity.Property(e => e.Ngaylap)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("NGAYLAP");
            entity.Property(e => e.Tongtien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TONGTIEN");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(50)
                .HasColumnName("TRANGTHAI");

            entity.HasOne(d => d.ManccNavigation).WithMany(p => p.Hoadonnhaps)
                .HasForeignKey(d => d.Mancc)
                .HasConstraintName("FK_HDNHAP_NCC");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.Hoadonnhaps)
                .HasForeignKey(d => d.Manv)
                .HasConstraintName("FK_HDNHAP_NHANVIEN");
        });

        modelBuilder.Entity<Hoadonxuat>(entity =>
        {
            entity.HasKey(e => e.Sohdxuat).HasName("PK__HOADONXU__C01FA3D9E593C142");

            entity.ToTable("HOADONXUAT");

            entity.Property(e => e.Sohdxuat)
                .HasMaxLength(20)
                .HasColumnName("SOHDXUAT");
            entity.Property(e => e.Makh)
                .HasMaxLength(20)
                .HasColumnName("MAKH");
            entity.Property(e => e.Manv)
                .HasMaxLength(20)
                .HasColumnName("MANV");
            entity.Property(e => e.Ngaylap)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("NGAYLAP");
            entity.Property(e => e.Tongtien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TONGTIEN");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(50)
                .HasColumnName("TRANGTHAI");
            entity.Property(e => e.TrangthaiDuyet)
                .HasDefaultValue(0)
                .HasColumnName("TRANGTHAI_DUYET");
            entity.Property(e => e.Vat)
                .HasDefaultValue(0.0)
                .HasColumnName("VAT");

            entity.HasOne(d => d.MakhNavigation).WithMany(p => p.Hoadonxuats)
                .HasForeignKey(d => d.Makh)
                .HasConstraintName("FK_HDXUAT_KHACHHANG");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.Hoadonxuats)
                .HasForeignKey(d => d.Manv)
                .HasConstraintName("FK_HDXUAT_NHANVIEN");
        });

        modelBuilder.Entity<Khachhang>(entity =>
        {
            entity.HasKey(e => e.Makh).HasName("PK__KHACHHAN__603F592C6288DE18");

            entity.ToTable("KHACHHANG");

            entity.Property(e => e.Makh)
                .HasMaxLength(20)
                .HasColumnName("MAKH");
            entity.Property(e => e.Diachi)
                .HasMaxLength(255)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Loaikh)
                .HasMaxLength(50)
                .HasColumnName("LOAIKH");
            entity.Property(e => e.Sdt)
                .HasMaxLength(20)
                .HasColumnName("SDT");
            entity.Property(e => e.Tenkh)
                .HasMaxLength(100)
                .HasColumnName("TENKH");
        });

        modelBuilder.Entity<Kho>(entity =>
        {
            entity.HasKey(e => e.Makho).HasName("PK__KHO__7AFB3D169ACDB5F8");

            entity.ToTable("KHO");

            entity.Property(e => e.Makho)
                .HasMaxLength(20)
                .HasColumnName("MAKHO");
            entity.Property(e => e.Diachi)
                .HasMaxLength(255)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Tenkho)
                .HasMaxLength(100)
                .HasColumnName("TENKHO");
        });

        modelBuilder.Entity<Loaisp>(entity =>
        {
            entity.HasKey(e => e.Maloai).HasName("PK__LOAISP__2F633F2352620F7E");

            entity.ToTable("LOAISP");

            entity.Property(e => e.Maloai)
                .HasMaxLength(20)
                .HasColumnName("MALOAI");
            entity.Property(e => e.Tenloai)
                .HasMaxLength(100)
                .HasColumnName("TENLOAI");
        });

        modelBuilder.Entity<Lohang>(entity =>
        {
            entity.HasKey(e => e.Malo).HasName("PK__LOHANG__603F415491FB82F7");

            entity.ToTable("LOHANG");

            entity.Property(e => e.Malo)
                .HasMaxLength(20)
                .HasColumnName("MALO");
            entity.Property(e => e.Hsd).HasColumnName("HSD");
            entity.Property(e => e.Masp)
                .HasMaxLength(20)
                .HasColumnName("MASP");
            entity.Property(e => e.Nhacungcap)
                .HasMaxLength(100)
                .HasColumnName("NHACUNGCAP");
            entity.Property(e => e.Nsx).HasColumnName("NSX");
            entity.Property(e => e.Sohieu)
                .HasMaxLength(50)
                .HasColumnName("SOHIEU");

            entity.HasOne(d => d.MaspNavigation).WithMany(p => p.Lohangs)
                .HasForeignKey(d => d.Masp)
                .HasConstraintName("FK_LOHANG_SANPHAM");
        });

        modelBuilder.Entity<Nhacungcap>(entity =>
        {
            entity.HasKey(e => e.Mancc).HasName("PK__NHACUNGC__7ABEA58203AC3EAD");

            entity.ToTable("NHACUNGCAP");

            entity.Property(e => e.Mancc)
                .HasMaxLength(20)
                .HasColumnName("MANCC");
            entity.Property(e => e.Diachi)
                .HasMaxLength(255)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Sdt)
                .HasMaxLength(20)
                .HasColumnName("SDT");
            entity.Property(e => e.Tenncc)
                .HasMaxLength(100)
                .HasColumnName("TENNCC");
        });

        modelBuilder.Entity<Nhanvien>(entity =>
        {
            entity.HasKey(e => e.Manv).HasName("PK__NHANVIEN__603F5114C9DDB6D3");

            entity.ToTable("NHANVIEN");

            entity.Property(e => e.Manv)
                .HasMaxLength(20)
                .HasColumnName("MANV");
            entity.Property(e => e.Avatar).HasColumnName("AVATAR");
            entity.Property(e => e.Cccd)
                .HasMaxLength(20)
                .HasColumnName("CCCD");
            entity.Property(e => e.Chucvu)
                .HasMaxLength(50)
                .HasColumnName("CHUCVU");
            entity.Property(e => e.Diachi)
                .HasMaxLength(255)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Gioitinh)
                .HasMaxLength(10)
                .HasColumnName("GIOITINH");
            entity.Property(e => e.Ngaysinh).HasColumnName("NGAYSINH");
            entity.Property(e => e.Sdt)
                .HasMaxLength(20)
                .HasColumnName("SDT");
            entity.Property(e => e.Tennv)
                .HasMaxLength(100)
                .HasColumnName("TENNV");
            entity.Property(e => e.Trangthai)
                .HasDefaultValue(1)
                .HasColumnName("TRANGTHAI");
        });

        modelBuilder.Entity<Phieunhap>(entity =>
        {
            entity.HasKey(e => e.Mapn).HasName("PK__PHIEUNHA__603F61CE8590E505");

            entity.ToTable("PHIEUNHAP");

            entity.Property(e => e.Mapn)
                .HasMaxLength(20)
                .HasColumnName("MAPN");
            entity.Property(e => e.Ghichu).HasColumnName("GHICHU");
            entity.Property(e => e.Makho)
                .HasMaxLength(20)
                .HasColumnName("MAKHO");
            entity.Property(e => e.Manv)
                .HasMaxLength(20)
                .HasColumnName("MANV");
            entity.Property(e => e.Ngaynhap)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("NGAYNHAP");
            entity.Property(e => e.Sohdnhap)
                .HasMaxLength(20)
                .HasColumnName("SOHDNHAP");

            entity.HasOne(d => d.MakhoNavigation).WithMany(p => p.Phieunhaps)
                .HasForeignKey(d => d.Makho)
                .HasConstraintName("FK_PHIEUNHAP_KHO");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.Phieunhaps)
                .HasForeignKey(d => d.Manv)
                .HasConstraintName("FK_PHIEUNHAP_NV");

            entity.HasOne(d => d.SohdnhapNavigation).WithMany(p => p.Phieunhaps)
                .HasForeignKey(d => d.Sohdnhap)
                .HasConstraintName("FK_PHIEUNHAP_HD");
        });

        modelBuilder.Entity<Phieuxuat>(entity =>
        {
            entity.HasKey(e => e.Mapx).HasName("PK__PHIEUXUA__603F61D8EA3F21DF");

            entity.ToTable("PHIEUXUAT");

            entity.Property(e => e.Mapx)
                .HasMaxLength(20)
                .HasColumnName("MAPX");
            entity.Property(e => e.Lydo).HasColumnName("LYDO");
            entity.Property(e => e.Makho)
                .HasMaxLength(20)
                .HasColumnName("MAKHO");
            entity.Property(e => e.Manv)
                .HasMaxLength(20)
                .HasColumnName("MANV");
            entity.Property(e => e.Ngayxuat)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("NGAYXUAT");
            entity.Property(e => e.Sohdxuat)
                .HasMaxLength(20)
                .HasColumnName("SOHDXUAT");

            entity.HasOne(d => d.MakhoNavigation).WithMany(p => p.Phieuxuats)
                .HasForeignKey(d => d.Makho)
                .HasConstraintName("FK_PHIEUXUAT_KHO");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.Phieuxuats)
                .HasForeignKey(d => d.Manv)
                .HasConstraintName("FK_PHIEUXUAT_NV");

            entity.HasOne(d => d.SohdxuatNavigation).WithMany(p => p.Phieuxuats)
                .HasForeignKey(d => d.Sohdxuat)
                .HasConstraintName("FK_PHIEUXUAT_HD");
        });

        modelBuilder.Entity<Sanpham>(entity =>
        {
            entity.HasKey(e => e.Masp).HasName("PK__SANPHAM__60228A3255D591C3");

            entity.ToTable("SANPHAM");

            entity.Property(e => e.Masp)
                .HasMaxLength(20)
                .HasColumnName("MASP");
            entity.Property(e => e.Dvt)
                .HasMaxLength(50)
                .HasColumnName("DVT");
            entity.Property(e => e.Ghichu).HasColumnName("GHICHU");
            entity.Property(e => e.Giaban)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("GIABAN");
            entity.Property(e => e.Hoatchat)
                .HasMaxLength(255)
                .HasColumnName("HOATCHAT");
            entity.Property(e => e.Maloai)
                .HasMaxLength(20)
                .HasColumnName("MALOAI");
            entity.Property(e => e.Nhacungcap)
                .HasMaxLength(100)
                .HasColumnName("NHACUNGCAP");
            entity.Property(e => e.Nuocsx)
                .HasMaxLength(100)
                .HasColumnName("NUOCSX");
            entity.Property(e => e.Tensp)
                .HasMaxLength(255)
                .HasColumnName("TENSP");

            entity.HasOne(d => d.MaloaiNavigation).WithMany(p => p.Sanphams)
                .HasForeignKey(d => d.Maloai)
                .HasConstraintName("FK_SANPHAM_LOAISP");
        });

        modelBuilder.Entity<Taikhoan>(entity =>
        {
            entity.HasKey(e => e.Manv).HasName("PK__TAIKHOAN__603F5114208AA67F");

            entity.ToTable("TAIKHOAN");

            entity.Property(e => e.Manv)
                .HasMaxLength(20)
                .HasColumnName("MANV");
            entity.Property(e => e.Matkhau)
                .HasMaxLength(100)
                .HasColumnName("MATKHAU");
            entity.Property(e => e.Quyenhan)
                .HasMaxLength(50)
                .HasColumnName("QUYENHAN");
            entity.Property(e => e.Trangthai).HasColumnName("TRANGTHAI");

            entity.HasOne(d => d.ManvNavigation).WithOne(p => p.Taikhoan)
                .HasForeignKey<Taikhoan>(d => d.Manv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TAIKHOAN_NHANVIEN");
        });

        modelBuilder.Entity<Thanhtoan>(entity =>
        {
            entity.HasKey(e => e.Matt).HasName("PK__THANHTOA__6023720F1C37569E");

            entity.ToTable("THANHTOAN");

            entity.Property(e => e.Matt)
                .HasMaxLength(20)
                .HasColumnName("MATT");
            entity.Property(e => e.Ghichu).HasColumnName("GHICHU");
            entity.Property(e => e.Manv)
                .HasMaxLength(20)
                .HasColumnName("MANV");
            entity.Property(e => e.Ngaythanhtoan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("NGAYTHANHTOAN");
            entity.Property(e => e.Phuongthuc)
                .HasMaxLength(50)
                .HasColumnName("PHUONGTHUC");
            entity.Property(e => e.Sohdxuat)
                .HasMaxLength(20)
                .HasColumnName("SOHDXUAT");
            entity.Property(e => e.Sotien)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("SOTIEN");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.Thanhtoans)
                .HasForeignKey(d => d.Manv)
                .HasConstraintName("FK_THANHTOAN_NV");

            entity.HasOne(d => d.SohdxuatNavigation).WithMany(p => p.Thanhtoans)
                .HasForeignKey(d => d.Sohdxuat)
                .HasConstraintName("FK_THANHTOAN_HD");
        });

        modelBuilder.Entity<Tonkho>(entity =>
        {
            entity.HasKey(e => new { e.Makho, e.Masp, e.Malo }).HasName("PK__TONKHO__19992AF411AB6EFA");

            entity.ToTable("TONKHO");

            entity.Property(e => e.Makho)
                .HasMaxLength(20)
                .HasColumnName("MAKHO");
            entity.Property(e => e.Masp)
                .HasMaxLength(20)
                .HasColumnName("MASP");
            entity.Property(e => e.Malo)
                .HasMaxLength(20)
                .HasColumnName("MALO");
            entity.Property(e => e.Soluongton)
                .HasDefaultValue(0)
                .HasColumnName("SOLUONGTON");

            entity.HasOne(d => d.MakhoNavigation).WithMany(p => p.Tonkhos)
                .HasForeignKey(d => d.Makho)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TONKHO_KHO");

            entity.HasOne(d => d.MaloNavigation).WithMany(p => p.Tonkhos)
                .HasForeignKey(d => d.Malo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TONKHO_LOHANG");

            entity.HasOne(d => d.MaspNavigation).WithMany(p => p.Tonkhos)
                .HasForeignKey(d => d.Masp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TONKHO_SANPHAM");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
