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

    // Khai báo các bảng
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

    // 1. Cấu hình kết nối SQLite
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite("Data Source=PharmaDB.db");

    // 2. Cấu hình bảng và khóa chính (Đã xóa các kiểu dữ liệu lỗi của SQL Server)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cthdnhap>(entity =>
        {
            entity.HasKey(e => new { e.Sohdnhap, e.Masp, e.Malo }); // Khóa chính phức hợp
            entity.ToTable("CTHDNHAP");
            entity.Property(e => e.Sohdnhap).HasMaxLength(8).HasColumnName("SOHDNHAP");
            entity.Property(e => e.Masp).HasMaxLength(6).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasMaxLength(6).HasColumnName("MALO");
            entity.Property(e => e.Dongianhap).HasColumnName("DONGIANHAP"); // SQLite tự hiểu là số thực
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");
        });

        modelBuilder.Entity<Cthdxuat>(entity =>
        {
            entity.HasKey(e => new { e.Sohdxuat, e.Masp, e.Malo });
            entity.ToTable("CTHDXUAT");
            entity.Property(e => e.Sohdxuat).HasMaxLength(8).HasColumnName("SOHDXUAT");
            entity.Property(e => e.Masp).HasMaxLength(6).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasMaxLength(6).HasColumnName("MALO");
            entity.Property(e => e.Dongiaban).HasColumnName("DONGIABAN");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");
        });

        modelBuilder.Entity<Hdnhap>(entity =>
        {
            entity.HasKey(e => e.Sohdnhap);
            entity.ToTable("HDNHAP");
            entity.Property(e => e.Sohdnhap).HasMaxLength(8).HasColumnName("SOHDNHAP");
            entity.Property(e => e.Ghichu).HasMaxLength(200).HasColumnName("GHICHU");
            entity.Property(e => e.Mancc).HasMaxLength(6).HasColumnName("MANCC");
            entity.Property(e => e.Manv).HasMaxLength(6).HasColumnName("MANV");
            entity.Property(e => e.Ngaynhap).HasColumnName("NGAYNHAP");
            entity.Property(e => e.Tongtien).HasColumnName("TONGTIEN");
        });

        modelBuilder.Entity<Hdxuat>(entity =>
        {
            entity.HasKey(e => e.Sohdxuat);
            entity.ToTable("HDXUAT");
            entity.Property(e => e.Sohdxuat).HasMaxLength(8).HasColumnName("SOHDXUAT");
            entity.Property(e => e.Makh).HasMaxLength(6).HasColumnName("MAKH");
            entity.Property(e => e.Manv).HasMaxLength(6).HasColumnName("MANV");
            entity.Property(e => e.Ngayxuat).HasColumnName("NGAYXUAT");
            entity.Property(e => e.Tongtien).HasColumnName("TONGTIEN");
            entity.Property(e => e.Vat).HasColumnName("VAT");
        });

        modelBuilder.Entity<Khachhang>(entity =>
        {
            entity.HasKey(e => e.Makh);
            entity.ToTable("KHACHHANG");
            entity.Property(e => e.Makh).HasMaxLength(6).HasColumnName("MAKH");
            entity.Property(e => e.Diachi).HasMaxLength(200).HasColumnName("DIACHI");
            entity.Property(e => e.Doanhso).HasDefaultValue(0m).HasColumnName("DOANHSO");
            entity.Property(e => e.Loaikh).HasMaxLength(50).HasColumnName("LOAIKH");
            entity.Property(e => e.Masothue).HasMaxLength(20).HasColumnName("MASOTHUE");
            entity.Property(e => e.Ngdk).HasColumnName("NGDK");
            entity.Property(e => e.Sdt).HasMaxLength(20).HasColumnName("SDT");
            entity.Property(e => e.Tenkh).HasMaxLength(100).HasColumnName("TENKH");
        });

        modelBuilder.Entity<Kho>(entity =>
        {
            entity.HasKey(e => e.Makho);
            entity.ToTable("KHO");
            entity.Property(e => e.Makho).HasMaxLength(6).HasColumnName("MAKHO");
            entity.Property(e => e.Diachi).HasMaxLength(200).HasColumnName("DIACHI");
            entity.Property(e => e.Tenkho).HasMaxLength(100).HasColumnName("TENKHO");
        });

        modelBuilder.Entity<Loaisp>(entity =>
        {
            entity.HasKey(e => e.Maloai);
            entity.ToTable("LOAISP");
            entity.Property(e => e.Maloai).HasMaxLength(6).HasColumnName("MALOAI");
            entity.Property(e => e.Tenloai).HasMaxLength(100).HasColumnName("TENLOAI");
        });

        modelBuilder.Entity<Lohang>(entity =>
        {
            entity.HasKey(e => e.Malo);
            entity.ToTable("LOHANG");
            entity.Property(e => e.Malo).HasMaxLength(6).HasColumnName("MALO");
            entity.Property(e => e.Hsd).HasColumnName("HSD");
            entity.Property(e => e.Masp).HasMaxLength(6).HasColumnName("MASP");
            entity.Property(e => e.Nsx).HasColumnName("NSX");
            entity.Property(e => e.Sohieu).HasMaxLength(50).HasColumnName("SOHIEU");
        });

        modelBuilder.Entity<Nhacungcap>(entity =>
        {
            entity.HasKey(e => e.Mancc);
            entity.ToTable("NHACUNGCAP");
            entity.Property(e => e.Mancc).HasMaxLength(6).HasColumnName("MANCC");
            entity.Property(e => e.Diachi).HasMaxLength(200).HasColumnName("DIACHI");
            entity.Property(e => e.Email).HasMaxLength(100).HasColumnName("EMAIL");
            entity.Property(e => e.Masothue).HasMaxLength(20).HasColumnName("MASOTHUE");
            entity.Property(e => e.Sdt).HasMaxLength(20).HasColumnName("SDT");
            entity.Property(e => e.Tenncc).HasMaxLength(100).HasColumnName("TENNCC");
        });

        modelBuilder.Entity<Nhanvien>(entity =>
        {
            entity.HasKey(e => e.Manv);
            entity.ToTable("NHANVIEN");
            entity.Property(e => e.Manv).HasMaxLength(6).HasColumnName("MANV");
            entity.Property(e => e.Chucvu).HasMaxLength(50).HasColumnName("CHUCVU");
            entity.Property(e => e.Diachi).HasMaxLength(200).HasColumnName("DIACHI");
            entity.Property(e => e.Email).HasMaxLength(100).HasColumnName("EMAIL");
            entity.Property(e => e.Ngaysinh).HasColumnName("NGAYSINH");
            entity.Property(e => e.Sdt).HasMaxLength(20).HasColumnName("SDT");
            entity.Property(e => e.Tennv).HasMaxLength(100).HasColumnName("TENNV");
        });

        modelBuilder.Entity<Sanpham>(entity =>
        {
            entity.HasKey(e => e.Masp);
            entity.ToTable("SANPHAM");
            entity.Property(e => e.Masp).HasMaxLength(6).HasColumnName("MASP");
            entity.Property(e => e.Dvt).HasMaxLength(50).HasColumnName("DVT");
            entity.Property(e => e.Giaban).HasColumnName("GIABAN");
            entity.Property(e => e.Hoatchat).HasMaxLength(200).HasColumnName("HOATCHAT");
            entity.Property(e => e.Maloai).HasMaxLength(6).HasColumnName("MALOAI");
            entity.Property(e => e.Nuocsx).HasMaxLength(100).HasColumnName("NUOCSX");
            entity.Property(e => e.Tensp).HasMaxLength(200).HasColumnName("TENSP");
            entity.Property(e => e.Trangthai).HasDefaultValue(1).HasColumnName("TRANGTHAI");
        });

        modelBuilder.Entity<Taikhoan>(entity =>
        {
            entity.HasKey(e => e.Idtk);
            entity.ToTable("TAIKHOAN");
            entity.Property(e => e.Idtk).HasColumnName("IDTK");
            entity.Property(e => e.Manv).HasMaxLength(6).HasColumnName("MANV");
            entity.Property(e => e.Matkhau).HasMaxLength(100).HasColumnName("MATKHAU");
            entity.Property(e => e.Quyenhan).HasMaxLength(30).HasColumnName("QUYENHAN");
            entity.Property(e => e.Tentk).HasMaxLength(50).HasColumnName("TENTK");
            entity.Property(e => e.Trangthai).HasColumnName("TRANGTHAI");
        });

        modelBuilder.Entity<Thanhtoan>(entity =>
        {
            entity.HasKey(e => e.Matt);
            entity.ToTable("THANHTOAN");
            entity.Property(e => e.Matt).HasMaxLength(8).HasColumnName("MATT");
            entity.Property(e => e.Ghichu).HasMaxLength(200).HasColumnName("GHICHU");
            entity.Property(e => e.Manv).HasMaxLength(6).HasColumnName("MANV");
            entity.Property(e => e.Ngaythanhtoan).HasColumnName("NGAYTHANHTOAN");
            entity.Property(e => e.Phuongthuc).HasMaxLength(50).HasColumnName("PHUONGTHUC");
            entity.Property(e => e.Sohdxuat).HasMaxLength(8).HasColumnName("SOHDXUAT");
            entity.Property(e => e.Sotien).HasColumnName("SOTIEN");
        });

        modelBuilder.Entity<Tonkho>(entity =>
        {
            entity.HasKey(e => new { e.Masp, e.Malo, e.Makho });
            entity.ToTable("TONKHO");
            entity.Property(e => e.Masp).HasMaxLength(6).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasMaxLength(6).HasColumnName("MALO");
            entity.Property(e => e.Makho).HasMaxLength(6).HasColumnName("MAKHO");
            entity.Property(e => e.Soluongton).HasColumnName("SOLUONGTON");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}