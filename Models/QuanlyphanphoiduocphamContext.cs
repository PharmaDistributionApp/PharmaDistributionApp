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
        // 1. CẤU HÌNH CÁC BẢNG CÓ KHÓA TỔ HỢP (ĐÂY LÀ CHỖ SỬA LỖI CỦA BẠN)

        // Bảng Chi tiết nhập (Khóa chính gồm 3 cột)
        modelBuilder.Entity<Cthdnhap>(entity =>
        {
            entity.ToTable("CTHDNHAP");
            // DÒNG QUAN TRỌNG NHẤT: Khai báo khóa chính tổ hợp
            entity.HasKey(e => new { e.Sohdnhap, e.Masp, e.Malo });

            entity.Property(e => e.Sohdnhap).HasColumnName("SOHDNHAP");
            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasColumnName("MALO");
            entity.Property(e => e.Dongianhap).HasColumnName("DONGIANHAP");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");
        });

        // Bảng Chi tiết xuất (Cũng cần khóa tổ hợp)
        modelBuilder.Entity<Cthdxuat>(entity =>
        {
            entity.ToTable("CTHDXUAT");
            entity.HasKey(e => new { e.Sohdxuat, e.Masp, e.Malo }); // Quan trọng

            entity.Property(e => e.Sohdxuat).HasColumnName("SOHDXUAT");
            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasColumnName("MALO");
            entity.Property(e => e.Dongiaban).HasColumnName("DONGIABAN");
            entity.Property(e => e.Soluong).HasColumnName("SOLUONG");
            entity.Property(e => e.Thanhtien).HasColumnName("THANHTIEN");
        });

        // Bảng Tồn kho (Cũng cần khóa tổ hợp)
        modelBuilder.Entity<Tonkho>(entity =>
        {
            entity.ToTable("TONKHO");
            entity.HasKey(e => new { e.Masp, e.Malo, e.Makho }); // Quan trọng

            entity.Property(e => e.Masp).HasColumnName("MASP");
            entity.Property(e => e.Malo).HasColumnName("MALO");
            entity.Property(e => e.Makho).HasColumnName("MAKHO");
            entity.Property(e => e.Soluongton).HasColumnName("SOLUONGTON");
        });

        // 2. CẤU HÌNH CÁC BẢNG CÒN LẠI (Khóa đơn)
        modelBuilder.Entity<Taikhoan>(entity => {
            entity.ToTable("TAIKHOAN");
            entity.HasKey(e => e.Manv); // Khóa chính là MANV
            entity.Property(e => e.Manv).HasColumnName("MANV");
            entity.Property(e => e.Matkhau).HasColumnName("MATKHAU");
            entity.Property(e => e.Quyenhan).HasColumnName("QUYENHAN");
            entity.Property(e => e.Trangthai).HasColumnName("TRANGTHAI");
        });

        modelBuilder.Entity<Nhanvien>(e => { e.ToTable("NHANVIEN"); e.HasKey(x => x.Manv); e.Property(x => x.Manv).HasColumnName("MANV"); e.Property(x => x.Avatar).HasColumnName("AVATAR"); });
        
        modelBuilder.Entity<Hoadonnhap>(entity =>
        {
            entity.ToTable("HOADONNHAP");
            entity.HasKey(e => e.Sohdnhap);
            entity.Property(e => e.Sohdnhap).HasColumnName("SOHDNHAP");

            // --- THÊM ĐOẠN NÀY ĐỂ FIX LỖI ---
            // Chỉ định rõ: Mối quan hệ với Nhà cung cấp sử dụng cột "Mancc"
            entity.HasOne<Nhacungcap>()         // Liên kết với bảng Nhacungcap
                  .WithMany()                   // Một NCC có nhiều hóa đơn
                  .HasForeignKey(d => d.Mancc); // Khóa ngoại là Mancc (thay vì NhacungcapMancc)
        });
        modelBuilder.Entity<Hoadonxuat>(entity =>
        {
            entity.ToTable("HOADONXUAT");
            entity.HasKey(e => e.Sohdxuat);
            entity.Property(e => e.Sohdxuat).HasColumnName("SOHDXUAT");

            // --- THÊM ĐOẠN NÀY ĐỂ FIX LỖI ---
            // Chỉ định rõ: Mối quan hệ với Khách hàng sử dụng cột "Makh"
            entity.HasOne<Khachhang>()          // Liên kết với bảng Khachhang
                  .WithMany()                   // Một KH có nhiều hóa đơn
                  .HasForeignKey(d => d.Makh);  // Khóa ngoại là Makh (thay vì KhachhangMakh)
        }); 
        
        modelBuilder.Entity<Phieunhap>(e => { e.ToTable("PHIEUNHAP"); e.HasKey(x => x.Mapn); e.Property(x => x.Mapn).HasColumnName("MAPN"); });
        modelBuilder.Entity<Phieuxuat>(e => { e.ToTable("PHIEUXUAT"); e.HasKey(x => x.Mapx); e.Property(x => x.Mapx).HasColumnName("MAPX"); });
        modelBuilder.Entity<Sanpham>(e => { e.ToTable("SANPHAM"); e.HasKey(x => x.Masp); e.Property(x => x.Masp).HasColumnName("MASP"); });
        modelBuilder.Entity<Kho>(e => { e.ToTable("KHO"); e.HasKey(x => x.Makho); e.Property(x => x.Makho).HasColumnName("MAKHO"); });
        modelBuilder.Entity<Lohang>(e => { e.ToTable("LOHANG"); e.HasKey(x => x.Malo); e.Property(x => x.Malo).HasColumnName("MALO"); });
        modelBuilder.Entity<Loaisp>(e => { e.ToTable("LOAISP"); e.HasKey(x => x.Maloai); e.Property(x => x.Maloai).HasColumnName("MALOAI"); });
        modelBuilder.Entity<Nhacungcap>(e => { e.ToTable("NHACUNGCAP"); e.HasKey(x => x.Mancc); e.Property(x => x.Mancc).HasColumnName("MANCC"); });
        modelBuilder.Entity<Khachhang>(e => { e.ToTable("KHACHHANG"); e.HasKey(x => x.Makh); e.Property(x => x.Makh).HasColumnName("MAKH"); });
        modelBuilder.Entity<Thanhtoan>(e => { e.ToTable("THANHTOAN"); e.HasKey(x => x.Matt); e.Property(x => x.Matt).HasColumnName("MATT"); });
    }
}