using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PharmaDistributionApp.Models
{
    public partial class QuanlyphanphoiduocphamContext : DbContext
    {
        public QuanlyphanphoiduocphamContext() { }
        public QuanlyphanphoiduocphamContext(DbContextOptions<QuanlyphanphoiduocphamContext> options) : base(options) { }

        // --- 1. KHAI BÁO DBSET ---
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
            // 1. CẤU HÌNH CHI TIẾT HÓA ĐƠN (KHÓA TỔ HỢP)
            modelBuilder.Entity<Cthdnhap>(entity =>
            {
                entity.ToTable("CTHDNHAP");
                entity.HasKey(e => new { e.Sohdnhap, e.Masp, e.Malo });

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
                entity.HasKey(e => new { e.Sohdxuat, e.Masp, e.Malo });

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
                entity.HasKey(e => new { e.Masp, e.Malo, e.Makho });

                entity.Property(e => e.Masp).HasColumnName("MASP");
                entity.Property(e => e.Malo).HasColumnName("MALO");
                entity.Property(e => e.Makho).HasColumnName("MAKHO");
                entity.Property(e => e.Soluongton).HasColumnName("SOLUONGTON");
            });

            // 2. CẤU HÌNH HÓA ĐƠN NHẬP (ĐÃ SỬA: FK NHÀ CUNG CẤP & NHÂN VIÊN)
            modelBuilder.Entity<Hoadonnhap>(entity =>
            {
                entity.ToTable("HOADONNHAP");
                entity.HasKey(e => e.Sohdnhap);

                entity.Property(e => e.Sohdnhap).HasColumnName("SOHDNHAP");
                entity.Property(e => e.Ngaylap).HasColumnName("NGAYLAP");
                entity.Property(e => e.Tongtien).HasColumnName("TONGTIEN");
                entity.Property(e => e.Manv).HasColumnName("MANV");
                entity.Property(e => e.Mancc).HasColumnName("MANCC");
                entity.Property(e => e.Trangthai).HasColumnName("TRANGTHAI");
                entity.Property(e => e.Vat).HasColumnName("VAT");
                entity.Property(e => e.Ghichu).HasColumnName("GHICHU");

                // Cấu hình Khóa Ngoại Nhà Cung Cấp
                entity.HasOne(d => d.ManccNavigation)
                      .WithMany()
                      .HasForeignKey(d => d.Mancc)
                      .HasConstraintName("FK_HOADONNHAP_NHACUNGCAP");

                // --- SỬA LỖI MỚI: Cấu hình Khóa Ngoại Nhân Viên ---
                entity.HasOne(d => d.ManvNavigation)
                      .WithMany()
                      .HasForeignKey(d => d.Manv)
                      .HasConstraintName("FK_HOADONNHAP_NHANVIEN");
            });

            // 3. CẤU HÌNH HÓA ĐƠN XUẤT (ĐÃ SỬA: FK KHÁCH HÀNG & NHÂN VIÊN)
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
                entity.Property(e => e.Trangthai).HasColumnName("TRANGTHAI");
                entity.Property(e => e.Ghichu).HasColumnName("GHICHU");

                // Cấu hình Khóa Ngoại Khách Hàng
                entity.HasOne(d => d.MakhNavigation)
                      .WithMany()
                      .HasForeignKey(d => d.Makh)
                      .HasConstraintName("FK_HOADONXUAT_KHACHHANG");

                // --- SỬA LỖI MỚI: Cấu hình Khóa Ngoại Nhân Viên ---
                entity.HasOne(d => d.ManvNavigation)
                      .WithMany()
                      .HasForeignKey(d => d.Manv)
                      .HasConstraintName("FK_HOADONXUAT_NHANVIEN");
            });

            // 4. CẤU HÌNH SẢN PHẨM & LOẠI SP
            modelBuilder.Entity<Sanpham>(entity =>
            {
                entity.ToTable("SANPHAM");
                entity.HasKey(x => x.Masp);
                entity.Property(x => x.Masp).HasColumnName("MASP");
                entity.Property(x => x.Tensp).HasColumnName("TENSP");
                entity.Property(x => x.Dvt).HasColumnName("DVT");
                entity.Property(x => x.Giaban).HasColumnName("GIABAN");
                entity.Property(x => x.Hoatchat).HasColumnName("HOATCHAT");
                entity.Property(x => x.Nuocsx).HasColumnName("NUOCSX");
                entity.Property(x => x.Nhacungcap).HasColumnName("NHACUNGCAP");
                entity.Property(x => x.Ghichu).HasColumnName("GHICHU");
                entity.Property(x => x.Maloai).HasColumnName("MALOAI");
                entity.Ignore("Trangthai");

                entity.HasOne<Loaisp>()
                      .WithMany(l => l.Sanphams)
                      .HasForeignKey(x => x.Maloai)
                      .HasConstraintName("FK_SANPHAM_LOAISP");
            });

            modelBuilder.Entity<Loaisp>(entity =>
            {
                entity.ToTable("LOAISP");
                entity.HasKey(e => e.Maloai);
                entity.Property(e => e.Maloai).HasColumnName("MALOAI");
                entity.Property(e => e.Tenloai).HasColumnName("TENLOAI");
            });

            // 5. CẤU HÌNH LÔ HÀNG
            modelBuilder.Entity<Lohang>(e => {
                e.ToTable("LOHANG");
                e.HasKey(x => x.Malo);
                e.Property(x => x.Malo).HasColumnName("MALO");
                e.Property(x => x.Masp).HasColumnName("MASP");
                e.Property(x => x.Sohieu).HasColumnName("SOHIEU");
                e.Property(x => x.Nsx).HasColumnName("NSX");
                e.Property(x => x.Hsd).HasColumnName("HSD");
                e.Property(x => x.Nhacungcap).HasColumnName("NHACUNGCAP");
            });

            // 6. CÁC BẢNG KHÁC
            modelBuilder.Entity<Taikhoan>(entity => {
                entity.ToTable("TAIKHOAN");
                entity.HasKey(e => e.Manv);
                entity.Property(e => e.Manv).HasColumnName("MANV");
                entity.Property(e => e.Matkhau).HasColumnName("MATKHAU");
                entity.Property(e => e.Quyenhan).HasColumnName("QUYENHAN");
                entity.Property(e => e.Trangthai).HasColumnName("TRANGTHAI");
            });

            modelBuilder.Entity<Nhanvien>(e => {
                e.ToTable("NHANVIEN");
                e.HasKey(x => x.Manv);
                e.Property(x => x.Manv).HasColumnName("MANV");
                e.Property(x => x.Avatar).HasColumnName("AVATAR");
            });

            modelBuilder.Entity<Phieunhap>(e => {
                e.ToTable("PHIEUNHAP");
                e.HasKey(x => x.Mapn);
                e.Property(x => x.Mapn).HasColumnName("MAPN");
            });

            modelBuilder.Entity<Phieuxuat>(e => {
                e.ToTable("PHIEUXUAT");
                e.HasKey(x => x.Mapx);
                e.Property(x => x.Mapx).HasColumnName("MAPX");
            });

            modelBuilder.Entity<Kho>(e => {
                e.ToTable("KHO");
                e.HasKey(x => x.Makho);
                e.Property(x => x.Makho).HasColumnName("MAKHO");
            });

            modelBuilder.Entity<Nhacungcap>(e => {
                e.ToTable("NHACUNGCAP");
                e.HasKey(x => x.Mancc);
                e.Property(x => x.Mancc).HasColumnName("MANCC");
            });

            modelBuilder.Entity<Khachhang>(e => {
                e.ToTable("KHACHHANG");
                e.HasKey(x => x.Makh);
                e.Property(x => x.Makh).HasColumnName("MAKH");
            });

            modelBuilder.Entity<Thanhtoan>(e => {
                e.ToTable("THANHTOAN");
                e.HasKey(x => x.Matt);
                e.Property(x => x.Matt).HasColumnName("MATT");
            });
        }
    }
}