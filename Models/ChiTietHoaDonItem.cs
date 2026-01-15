using System;

namespace PharmaDistributionApp.Models
{
    public class ChiTietHoaDonItem
    {
        public int STT { get; set; }
        public string MaSP { get; set; } = "";
        public string TenSP { get; set; } = "";
        public string DonVi { get; set; } = "";
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }

        // Tự động tính Thành tiền = Số lượng * Đơn giá
        public decimal ThanhTien => SoLuong * DonGia;

        // Format hiển thị tiền tệ (Thêm chữ 'đ' và dấu phẩy ngăn cách hàng nghìn)
        public string DonGiaStr => string.Format("{0:N0} đ", DonGia);
        public string ThanhTienStr => string.Format("{0:N0} đ", ThanhTien);
    }
}