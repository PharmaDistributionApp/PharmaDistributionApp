using System;

namespace PharmaDistributionApp.Views // Đảm bảo Namespace trùng với nơi bạn dùng
{
    public class InvoiceViewModel
    {
        // Mã hóa đơn (Lấy từ SOHDNHAP hoặc SOHDXUAT)
        public string MaHD { get; set; }

        // Tên Khách hàng hoặc Nhà cung cấp (Lấy từ MAKH hoặc MANCC)
        public string DoiTac { get; set; }

        // Ngày lập hóa đơn
        public DateTime NgayLap { get; set; }

        // Tổng tiền
        public decimal TongTien { get; set; }

        // Trạng thái (Đã thanh toán, Chờ thanh toán, Đã hủy...)
        public string TrangThai { get; set; }

        // Loại hóa đơn: "Nhập" hoặc "Xuất" (Để code biết đang ở tab nào)
        public string LoaiHD { get; set; }

        // Property chỉ đọc: Tự động format số tiền thành chuỗi (VD: 1,000,000 đ)
        // Dùng cái này để Binding lên DataGrid cho đẹp
        public string TongTienString
        {
            get
            {
                return string.Format("{0:N0} đ", TongTien);
            }
        }
    }
}