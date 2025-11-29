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

    public virtual DbSet<Hdnhap> Hdnhaps { get; set; }

    public virtual DbSet<Hdxuat> Hdxuats { get; set; }

    public virtual DbSet<Khachhang> Khachhangs { get; set; }

    public virtual DbSet<Kho> Khos { get; set; }

    public virtual DbSet<Loaisp> Loaisps { get; set; }

    public virtual DbSet<Lohang> Lohangs { get; set; }

    public virtual DbSet<Nhacungcap> Nhacungcaps { get; set; }

    public virtual DbSet<Nhanvien> Nhanviens { get; set; }

    public virtual DbSet<Sanpham> Sanphams { get; set; }

    public virtual DbSet<Taikhoan> Taikhoans { get; set; }

    public virtual DbSet<Thanhtoan> Thanhtoans { get; set; }

    public virtual DbSet<Tonkho> Tonkhos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.;Database=QUANLYPHANPHOIDUOCPHAM;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cthdnhap>(entity =>
        {
            entity.HasKey(e => new { e.Sohdnhap, e.Masp, e.Malo }).HasName("PK__CTHDNHAP__03E8DC7FBE568B65");

            entity.ToTable("CTHDNHAP");

            entity.Property(e => e.Sohdnhap)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("SOHDNHAP");
            entity.Property(e => e.Masp)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MASP");
            entity.Property(e => e.Malo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MALO");
            entity.Property(e => e.Dongianhap)
                .HasColumnType("money")
                .HasColumnName("DONGIANHAP");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien)
                .HasColumnType("money")
                .HasColumnName("THANHTIEN");

            entity.HasOne(d => d.MaloNavigation).WithMany(p => p.Cthdnhaps)
                .HasForeignKey(d => d.Malo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHDNHAP_LOHANG");

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
            entity.HasKey(e => new { e.Sohdxuat, e.Masp, e.Malo }).HasName("PK__CTHDXUAT__A37DB43BEAF51435");

            entity.ToTable("CTHDXUAT");

            entity.Property(e => e.Sohdxuat)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("SOHDXUAT");
            entity.Property(e => e.Masp)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MASP");
            entity.Property(e => e.Malo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MALO");
            entity.Property(e => e.Dongiaban)
                .HasColumnType("money")
                .HasColumnName("DONGIABAN");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien)
                .HasColumnType("money")
                .HasColumnName("THANHTIEN");

            entity.HasOne(d => d.MaloNavigation).WithMany(p => p.Cthdxuats)
                .HasForeignKey(d => d.Malo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHDXUAT_LOHANG");

            entity.HasOne(d => d.MaspNavigation).WithMany(p => p.Cthdxuats)
                .HasForeignKey(d => d.Masp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHDXUAT_SP");

            entity.HasOne(d => d.SohdxuatNavigation).WithMany(p => p.Cthdxuats)
                .HasForeignKey(d => d.Sohdxuat)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CTHDXUAT_HD");
        });

        modelBuilder.Entity<Hdnhap>(entity =>
        {
            entity.HasKey(e => e.Sohdnhap).HasName("PK__HDNHAP__608ACB9DED61986E");

            entity.ToTable("HDNHAP");

            entity.Property(e => e.Sohdnhap)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("SOHDNHAP");
            entity.Property(e => e.Ghichu)
                .HasMaxLength(200)
                .HasColumnName("GHICHU");
            entity.Property(e => e.Mancc)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANCC");
            entity.Property(e => e.Manv)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANV");
            entity.Property(e => e.Ngaynhap).HasColumnName("NGAYNHAP");
            entity.Property(e => e.Tongtien)
                .HasColumnType("money")
                .HasColumnName("TONGTIEN");

            entity.HasOne(d => d.ManccNavigation).WithMany(p => p.Hdnhaps)
                .HasForeignKey(d => d.Mancc)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HDNHAP_NCC");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.Hdnhaps)
                .HasForeignKey(d => d.Manv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HDNHAP_NHANVIEN");
        });

        modelBuilder.Entity<Hdxuat>(entity =>
        {
            entity.HasKey(e => e.Sohdxuat).HasName("PK__HDXUAT__C01FA3D9858862BE");

            entity.ToTable("HDXUAT");

            entity.Property(e => e.Sohdxuat)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("SOHDXUAT");
            entity.Property(e => e.Makh)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAKH");
            entity.Property(e => e.Manv)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANV");
            entity.Property(e => e.Ngayxuat).HasColumnName("NGAYXUAT");
            entity.Property(e => e.Tongtien)
                .HasColumnType("money")
                .HasColumnName("TONGTIEN");
            entity.Property(e => e.Vat).HasColumnName("VAT");

            entity.HasOne(d => d.MakhNavigation).WithMany(p => p.Hdxuats)
                .HasForeignKey(d => d.Makh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HDXUAT_KH");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.Hdxuats)
                .HasForeignKey(d => d.Manv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HDXUAT_NV");
        });

        modelBuilder.Entity<Khachhang>(entity =>
        {
            entity.HasKey(e => e.Makh).HasName("PK__KHACHHAN__603F592C0378C13E");

            entity.ToTable("KHACHHANG");

            entity.Property(e => e.Makh)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAKH");
            entity.Property(e => e.Diachi)
                .HasMaxLength(200)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Doanhso)
                .HasDefaultValue(0m)
                .HasColumnType("money")
                .HasColumnName("DOANHSO");
            entity.Property(e => e.Loaikh)
                .HasMaxLength(50)
                .HasColumnName("LOAIKH");
            entity.Property(e => e.Masothue)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("MASOTHUE");
            entity.Property(e => e.Ngdk).HasColumnName("NGDK");
            entity.Property(e => e.Sdt)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("SDT");
            entity.Property(e => e.Tenkh)
                .HasMaxLength(100)
                .HasColumnName("TENKH");
        });

        modelBuilder.Entity<Kho>(entity =>
        {
            entity.HasKey(e => e.Makho).HasName("PK__KHO__7AFB3D16AD7EC71F");

            entity.ToTable("KHO");

            entity.Property(e => e.Makho)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAKHO");
            entity.Property(e => e.Diachi)
                .HasMaxLength(200)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Tenkho)
                .HasMaxLength(100)
                .HasColumnName("TENKHO");
        });

        modelBuilder.Entity<Loaisp>(entity =>
        {
            entity.HasKey(e => e.Maloai).HasName("PK__LOAISP__2F633F23925BB32E");

            entity.ToTable("LOAISP");

            entity.Property(e => e.Maloai)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MALOAI");
            entity.Property(e => e.Tenloai)
                .HasMaxLength(100)
                .HasColumnName("TENLOAI");
        });

        modelBuilder.Entity<Lohang>(entity =>
        {
            entity.HasKey(e => e.Malo).HasName("PK__LOHANG__603F4154F461BA1F");

            entity.ToTable("LOHANG");

            entity.Property(e => e.Malo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MALO");
            entity.Property(e => e.Hsd).HasColumnName("HSD");
            entity.Property(e => e.Masp)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MASP");
            entity.Property(e => e.Nsx).HasColumnName("NSX");
            entity.Property(e => e.Sohieu)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SOHIEU");

            entity.HasOne(d => d.MaspNavigation).WithMany(p => p.Lohangs)
                .HasForeignKey(d => d.Masp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LOHANG_SANPHAM");
        });

        modelBuilder.Entity<Nhacungcap>(entity =>
        {
            entity.HasKey(e => e.Mancc).HasName("PK__NHACUNGC__7ABEA582258235A1");

            entity.ToTable("NHACUNGCAP");

            entity.Property(e => e.Mancc)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANCC");
            entity.Property(e => e.Diachi)
                .HasMaxLength(200)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Masothue)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("MASOTHUE");
            entity.Property(e => e.Sdt)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("SDT");
            entity.Property(e => e.Tenncc)
                .HasMaxLength(100)
                .HasColumnName("TENNCC");
        });

        modelBuilder.Entity<Nhanvien>(entity =>
        {
            entity.HasKey(e => e.Manv).HasName("PK__NHANVIEN__603F51149FE17C1C");

            entity.ToTable("NHANVIEN");

            entity.Property(e => e.Manv)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANV");
            entity.Property(e => e.Chucvu)
                .HasMaxLength(50)
                .HasColumnName("CHUCVU");
            entity.Property(e => e.Diachi)
                .HasMaxLength(200)
                .HasColumnName("DIACHI");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            entity.Property(e => e.Ngaysinh).HasColumnName("NGAYSINH");
            entity.Property(e => e.Sdt)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("SDT");
            entity.Property(e => e.Tennv)
                .HasMaxLength(100)
                .HasColumnName("TENNV");
        });

        modelBuilder.Entity<Sanpham>(entity =>
        {
            entity.HasKey(e => e.Masp).HasName("PK__SANPHAM__60228A32455231C8");

            entity.ToTable("SANPHAM");

            entity.Property(e => e.Masp)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MASP");
            entity.Property(e => e.Dvt)
                .HasMaxLength(50)
                .HasColumnName("DVT");
            entity.Property(e => e.Giaban)
                .HasColumnType("money")
                .HasColumnName("GIABAN");
            entity.Property(e => e.Hoatchat)
                .HasMaxLength(200)
                .HasColumnName("HOATCHAT");
            entity.Property(e => e.Maloai)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MALOAI");
            entity.Property(e => e.Nuocsx)
                .HasMaxLength(100)
                .HasColumnName("NUOCSX");
            entity.Property(e => e.Tensp)
                .HasMaxLength(200)
                .HasColumnName("TENSP");
            entity.Property(e => e.Trangthai)
                .HasDefaultValue(1)
                .HasColumnName("TRANGTHAI");

            entity.HasOne(d => d.MaloaiNavigation).WithMany(p => p.Sanphams)
                .HasForeignKey(d => d.Maloai)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SANPHAM_LOAISP");
        });

        modelBuilder.Entity<Taikhoan>(entity =>
        {
            entity.HasKey(e => e.Idtk).HasName("PK__TAIKHOAN__B87C3A835E706BA0");

            entity.ToTable("TAIKHOAN");

            entity.Property(e => e.Idtk).HasColumnName("IDTK");
            entity.Property(e => e.Manv)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANV");
            entity.Property(e => e.Matkhau)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("MATKHAU");
            entity.Property(e => e.Quyenhan)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("QUYENHAN");
            entity.Property(e => e.Tentk)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TENTK");
            entity.Property(e => e.Trangthai).HasColumnName("TRANGTHAI");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.Taikhoans)
                .HasForeignKey(d => d.Manv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TAIKHOAN_NHANVIEN");
        });

        modelBuilder.Entity<Thanhtoan>(entity =>
        {
            entity.HasKey(e => e.Matt).HasName("PK__THANHTOA__6023720F38C5EADE");

            entity.ToTable("THANHTOAN");

            entity.Property(e => e.Matt)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MATT");
            entity.Property(e => e.Ghichu)
                .HasMaxLength(200)
                .HasColumnName("GHICHU");
            entity.Property(e => e.Manv)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MANV");
            entity.Property(e => e.Ngaythanhtoan).HasColumnName("NGAYTHANHTOAN");
            entity.Property(e => e.Phuongthuc)
                .HasMaxLength(50)
                .HasColumnName("PHUONGTHUC");
            entity.Property(e => e.Sohdxuat)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("SOHDXUAT");
            entity.Property(e => e.Sotien)
                .HasColumnType("money")
                .HasColumnName("SOTIEN");

            entity.HasOne(d => d.ManvNavigation).WithMany(p => p.Thanhtoans)
                .HasForeignKey(d => d.Manv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_THANHTOAN_NV");

            entity.HasOne(d => d.SohdxuatNavigation).WithMany(p => p.Thanhtoans)
                .HasForeignKey(d => d.Sohdxuat)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_THANHTOAN_HD");
        });

        modelBuilder.Entity<Tonkho>(entity =>
        {
            entity.HasKey(e => new { e.Masp, e.Malo, e.Makho }).HasName("PK__TONKHO__215B851A9E21D3D9");

            entity.ToTable("TONKHO");

            entity.Property(e => e.Masp)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MASP");
            entity.Property(e => e.Malo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MALO");
            entity.Property(e => e.Makho)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("MAKHO");
            entity.Property(e => e.Soluongton).HasColumnName("SOLUONGTON");

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
                .HasConstraintName("FK_TONKHO_SP");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
