using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PharmaDistributionApp.Services
{
    public enum EmployeeStatus
    {
        Resigned = 0,   // Đã nghỉ việc
        Active = 1,     // Đang làm việc
        OnLeave = 2     // Tạm nghỉ
    }
    public class Employee
    {
        public string Manv { get; set; }      
        public string Tennv { get; set; }       
        public string GioiTinh { get; set; }
        public string Cccd { get; set; }
        public string Chucvu { get; set; }      
        public string Email { get; set; }       
        public string Sdt { get; set; }       
        public string Diachi { get; set; }      
        public DateTime? Ngaysinh { get; set; }
        public int TrangThai { get; set; }
        public byte[] AvatarBlob { get; set; }  // Dữ liệu BLOB từ SQL

        public BitmapImage AvatarSource
        {
            get
            {
                if (AvatarBlob == null || AvatarBlob.Length == 0) return null;
                try
                {
                    using (var ms = new System.IO.MemoryStream(AvatarBlob))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                        return image;
                    }
                }
                catch { return null; }
            }
        }
        public string TrangThaiHienThi
        {
            get
            {
                // Ép kiểu int sang Enum để switch
                switch ((EmployeeStatus)TrangThai)
                {
                    case EmployeeStatus.Active:
                        return "Đang hoạt động";
                    case EmployeeStatus.Resigned:
                        return "Đã nghỉ việc";
                    case EmployeeStatus.OnLeave:
                        return "Tạm nghỉ";
                    default:
                        return "Không xác định";
                }
            }
        }
        public string TrangThaiMau
        {
            get
            {
                switch ((EmployeeStatus)TrangThai)
                {
                    case EmployeeStatus.Active: return "#4CAF50"; // Xanh lá
                    case EmployeeStatus.Resigned: return "#F44336"; // Đỏ
                    case EmployeeStatus.OnLeave: return "#FFC107"; // Vàng cam
                    default: return "Gray";
                }
            }
        }
    }
}
