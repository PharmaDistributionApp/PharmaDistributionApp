using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Cần thêm dòng này cho MouseButtonEventArgs
using System.Windows.Media;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp.Views.NCCView
{
    /// <summary>
    /// Interaction logic for NhaCungCapControl.xaml
    /// </summary>
    public partial class NhaCungCapControl : UserControl
    {
        public NhaCungCapControl()
        {
            InitializeComponent();
            LoadData();
        }

        // ============================================================
        // 1. HÀM TẢI DỮ LIỆU TỪ DATABASE
        // ============================================================
        private void LoadData()
        {
            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var listNCC = context.Nhacungcaps.OrderBy(x => x.Tenncc).ToList();
                    dgvNhaCungCap.ItemsSource = listNCC;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // 2. XỬ LÝ TÌM KIẾM
        // ============================================================
        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtTimKiem.Text == "Tìm kiếm..." || dgvNhaCungCap == null) return;

            string keyword = txtTimKiem.Text.ToLower().Trim();

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    var query = context.Nhacungcaps.AsQueryable();

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        query = query.Where(x =>
                            x.Mancc.ToLower().Contains(keyword) ||
                            x.Tenncc.ToLower().Contains(keyword));
                    }
                    dgvNhaCungCap.ItemsSource = query.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi tìm kiếm: " + ex.Message);
            }
        }

        private void txtTimKiem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtTimKiem.Text == "Tìm kiếm...")
            {
                txtTimKiem.Text = "";
                txtTimKiem.Foreground = Brushes.Black;
            }
        }

        private void txtTimKiem_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTimKiem.Text))
            {
                txtTimKiem.Text = "Tìm kiếm...";
                txtTimKiem.Foreground = Brushes.Gray;
            }
        }

        // ============================================================
        // 3. CÁC SỰ KIỆN THAO TÁC (QUAN TRỌNG)
        // ============================================================

        // Nút Thêm Mới
        private void BtnThemMoi_Click(object sender, RoutedEventArgs e)
        {
            // Mở cửa sổ mode Thêm mới (không truyền tham số)
            var addWindow = new ThemSuaNhaCungCapWindow();

            // ShowDialog sẽ dừng code tại đây cho đến khi cửa sổ kia đóng lại
            if (addWindow.ShowDialog() == true)
            {
                // Nếu Lưu thành công thì tải lại danh sách
                LoadData();
            }
        }

        // Nút Xuất Excel
        private void BtnXuatExcel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng xuất Excel đang được phát triển.", "Thông báo");
        }

        // --- MỚI: Xử lý Click đúp vào dòng để xem chi tiết ---
        private void dgvNhaCungCap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var ncc = dgvNhaCungCap.SelectedItem as Nhacungcap;
            if (ncc != null)
            {
                // Mở cửa sổ Chi tiết mới tạo
                var detailWindow = new ChiTietNhaCungCapWindow(ncc);
                detailWindow.ShowDialog();
            }
        }

        // --- MỚI: Xử lý nút 3 chấm để mở Menu ---
        private void BtnHanhDong_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            // Mở ContextMenu gắn liền với nút đó
            if (btn != null && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn; // Đặt vị trí menu ngay tại nút
                btn.ContextMenu.IsOpen = true; // Mở menu
            }
        }

        // --- MỚI: Xử lý nút Sửa (trong Menu) ---
        private void BtnSua_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var ncc = menuItem.DataContext as Nhacungcap;

            if (ncc != null)
            {
                // Mở cửa sổ mode Sửa (truyền ncc vào)
                var editWindow = new ThemSuaNhaCungCapWindow(ncc);

                if (editWindow.ShowDialog() == true)
                {
                    LoadData(); // Tải lại danh sách sau khi sửa xong
                }
            }
        }

        // --- MỚI: Xử lý nút Xóa (trong Menu) ---
        private void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var ncc = menuItem.DataContext as Nhacungcap;

            if (ncc != null)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa '{ncc.Tenncc}' không?",
                                             "Xác nhận xóa",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new QuanlyphanphoiduocphamContext())
                        {
                            // Tìm đối tượng trong DB để xóa
                            var dbNcc = context.Nhacungcaps.Find(ncc.Mancc);
                            if (dbNcc != null)
                            {
                                context.Nhacungcaps.Remove(dbNcc);
                                context.SaveChanges(); // Lưu thay đổi

                                MessageBox.Show("Đã xóa thành công!", "Thông báo");
                                LoadData(); // Tải lại danh sách
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể xóa (Có thể NCC này đang dính líu đến dữ liệu nhập hàng):\n" + ex.Message, "Lỗi");
                    }
                }
            }
        }
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus(); // Lệnh này giúp bỏ focus khỏi ô tìm kiếm
        }

    }
}