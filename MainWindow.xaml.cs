using System;
using System.Linq;
using System.Windows;
using PharmaDistributionApp.Models; // Namespace chứa EF Core Context

namespace PharmaDistributionApp
{
    public partial class MainWindow : Window
    {
        // BIẾN QUAN TRỌNG: Lưu mã nhân viên đang đăng nhập.
        // AccountControl sẽ đọc biến này để lấy thông tin.
        public static string CurrentMaNV { get; set; } = "NV001";

        public MainWindow()
        {
            InitializeComponent();

            // Tải thông tin người dùng lên Sidebar ngay khi mở
            LoadSidebarInfo();
        }

        private void LoadSidebarInfo()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    // Tìm nhân viên theo mã
                    var nv = context.Nhanviens.FirstOrDefault(x => x.Manv == CurrentMaNV);

                    if (nv != null)
                    {
                        // Hiển thị tên lên Sidebar
                        txbUserName.Text = nv.Tennv;

                        // Hiển thị chức vụ (nếu có)
                        if (!string.IsNullOrEmpty(nv.Chucvu))
                        {
                            txbUserRole.Text = nv.Chucvu;
                        }
                    }
                    else
                    {
                        txbUserName.Text = "Khách";
                        txbUserRole.Text = "Unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                txbUserName.Text = "Offline";
                MessageBox.Show("Lỗi kết nối CSDL: " + ex.Message);
            }
        }
    }
}