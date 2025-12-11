using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PharmaDistributionApp.Models;

public partial class QuanlyphanphoiduocphamContext : DbContext
{
    public QuanlyphanphoiduocphamContext() { }
    public QuanlyphanphoiduocphamContext(DbContextOptions<QuanlyphanphoiduocphamContext> options) : base(options) { }

    // --- 1. KHAI BÁO DBSET (Cập nhật tên class mới) ---
    public virtual DbSet<Cthdnhap> Cthdnhaps { get; set; }
    public virtual DbSet<Cthdxuat> Cthdxuats { get; set; }
    public virtual DbSet<Hoadonnhap> Hoadonnhaps { get; set; }
    public virtual DbSet<Hoadonxuat> Hoadonxuats { get; set; }
    public virtual DbSet<Phieunhap> Phieunhaps { get; set; }
    public virtual DbSet<Phieuxuat> Phieuxuats { get; set; }
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

    // --- 2. CẤU HÌNH SQLITE ---
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=PharmaDB.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- 3. CẤU HÌNH CÁC BẢNG QUAN TRỌNG ---

        // BẢNG TÀI KHOẢN (Manv là khóa chính)
        modelBuilder.Entity<Taikhoan>(entity =>
        {
            entity.ToTable("TAIKHOAN");
            entity.HasKey(e => e.Manv);
            entity.Property(e => e.Manv).HasColumnName("MANV");
            entity.Property(e => e.Matkhau).HasColumnName("MATKHAU");
            entity.Property(e => e.Quyenhan).HasColumnName("QUYENHAN");
            entity.Property(e => e.Trangthai).HasColumnName("TRANGTHAI");
        });

        // BẢNG CHI TIẾT (Sửa lỗi "requires a primary key")
        modelBuilder.Entity<Cthdnhap>(entity =>
        {
            entity.ToTable("CTHDNHAP");
            entity.HasKey(e => new { e.Sohdnhap, e.Masp, e.Malo }); // Khóa tổ hợp
            entity.Property(e => e.Sohdnhap).HasColumnName("SOHDNHAP");
            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasColumnName("MALO");
            entity.Property(e => e.Dongianhap).HasColumnName("DONGIANHAP");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");
        });

        modelBuilder.Entity<Cthdxuat>(entity =>
        {
            entity.ToTable("CTHDXUAT");
            entity.HasKey(e => new { e.Sohdxuat, e.Masp, e.Malo }); // Khóa tổ hợp
            entity.Property(e => e.Sohdxuat).HasColumnName("SOHDXUAT");
            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasColumnName("MALO");
            entity.Property(e => e.Dongiaban).HasColumnName("DONGIABAN");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");
        });

        modelBuilder.Entity<Tonkho>(entity =>
        {
            entity.ToTable("TONKHO");
            entity.HasKey(e => new { e.Masp, e.Malo, e.Makho }); // Khóa tổ hợp
            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasColumnName("MALO");
            entity.Property(e => e.Makho).HasColumnName("MAKHO");
            entity.Property(e => e.Soluongton).HasColumnName("SOLUONGTON");
        });

        // CÁC BẢNG KHÁC (Map tên cột cho chuẩn)
        modelBuilder.Entity<Hoadonnhap>(e => { e.ToTable("HOADONNHAP"); e.HasKey(x => x.Sohdnhap); e.Property(x => x.Sohdnhap).HasColumnName("SOHDNHAP"); e.Property(x => x.Ngaylap).HasColumnName("NGAYLAP"); e.Property(x => x.Tongtien).HasColumnName("TONGTIEN"); e.Property(x => x.Manv).HasColumnName("MANV"); e.Property(x => x.Mancc).HasColumnName("MANCC"); e.Property(x => x.Ghichu).HasColumnName("GHICHU"); });
        modelBuilder.Entity<Hoadonxuat>(e => { e.ToTable("HOADONXUAT"); e.HasKey(x => x.Sohdxuat); e.Property(x => x.Sohdxuat).HasColumnName("SOHDXUAT"); e.Property(x => x.Ngaylap).HasColumnName("NGAYLAP"); e.Property(x => x.Tongtien).HasColumnName("TONGTIEN"); e.Property(x => x.Vat).HasColumnName("VAT"); e.Property(x => x.Manv).HasColumnName("MANV"); e.Property(x => x.Makh).HasColumnName("MAKH"); });
        modelBuilder.Entity<Phieunhap>(e => { e.ToTable("PHIEUNHAP"); e.HasKey(x => x.Mapn); e.Property(x => x.Mapn).HasColumnName("MAPN"); e.Property(x => x.Sohdnhap).HasColumnName("SOHDNHAP"); e.Property(x => x.Makho).HasColumnName("MAKHO"); e.Property(x => x.Manv).HasColumnName("MANV"); e.Property(x => x.Ngaynhap).HasColumnName("NGAYNHAP"); e.Property(x => x.Ghichu).HasColumnName("GHICHU"); });
        modelBuilder.Entity<Phieuxuat>(e => { e.ToTable("PHIEUXUAT"); e.HasKey(x => x.Mapx); e.Property(x => x.Mapx).HasColumnName("MAPX"); e.Property(x => x.Sohdxuat).HasColumnName("SOHDXUAT"); e.Property(x => x.Makho).HasColumnName("MAKHO"); e.Property(x => x.Manv).HasColumnName("MANV"); e.Property(x => x.Ngayxuat).HasColumnName("NGAYXUAT"); e.Property(x => x.Lydo).HasColumnName("LYDO"); });

        // Các bảng danh mục (Giản lược để code gọn, bạn thêm đầy đủ property nếu cần)
        modelBuilder.Entity<Nhanvien>(e => { e.ToTable("NHANVIEN"); e.HasKey(x => x.Manv); e.Property(x => x.Manv).HasColumnName("MANV"); e.Property(x => x.Email).HasColumnName("EMAIL"); e.Property(x => x.Tennv).HasColumnName("TENNV"); });
        modelBuilder.Entity<Khachhang>(e => { e.ToTable("KHACHHANG"); e.HasKey(x => x.Makh); e.Property(x => x.Makh).HasColumnName("MAKH"); });
        modelBuilder.Entity<Kho>(e => { e.ToTable("KHO"); e.HasKey(x => x.Makho); e.Property(x => x.Makho).HasColumnName("MAKHO"); });
        modelBuilder.Entity<Sanpham>(e => { e.ToTable("SANPHAM"); e.HasKey(x => x.Masp); e.Property(x => x.Masp).HasColumnName("MASP"); });
        modelBuilder.Entity<Lohang>(e => { e.ToTable("LOHANG"); e.HasKey(x => x.Malo); e.Property(x => x.Malo).HasColumnName("MALO"); });
    }
}