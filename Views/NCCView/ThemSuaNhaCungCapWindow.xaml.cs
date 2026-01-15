using System;
using System.Linq;
using System.Windows;
using PharmaDistributionApp.Models; // Namespace chứa Model của bạn

namespace PharmaDistributionApp.Views.NCCView
{
    public partial class ThemSuaNhaCungCapWindow : Window
    {
        private Nhacungcap _nccHienTai = null; // Biến lưu đối tượng đang sửa

        public ThemSuaNhaCungCapWindow(Nhacungcap ncc = null)
        {
            InitializeComponent();
            _nccHienTai = ncc;

            if (_nccHienTai != null) // CHẾ ĐỘ SỬA
            {
                this.Title = "Chỉnh sửa Nhà cung cấp";

                // Đổ dữ liệu vào các ô
                txtMaNCC.Text = _nccHienTai.Mancc;
                txtTenNCC.Text = _nccHienTai.Tenncc;
                txtSdt.Text = _nccHienTai.Sdt;
                txtEmail.Text = _nccHienTai.Email;
                txtDiaChi.Text = _nccHienTai.Diachi;

                // Khi sửa thì KHÔNG cho sửa Mã (Primary Key)
                txtMaNCC.IsEnabled = false;
                txtMaNCC.Background = System.Windows.Media.Brushes.WhiteSmoke;
            }
            else // CHẾ ĐỘ THÊM MỚI
            {
                this.Title = "Thêm mới Nhà cung cấp";
                txtMaNCC.IsEnabled = true;
            }
        }

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra dữ liệu nhập
            if (string.IsNullOrWhiteSpace(txtMaNCC.Text) || string.IsNullOrWhiteSpace(txtTenNCC.Text))
            {
                MessageBox.Show("Vui lòng nhập Mã và Tên nhà cung cấp!", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new QuanlyphanphoiduocphamContext())
                {
                    if (_nccHienTai == null)
                    {
                        // --- LOGIC THÊM MỚI ---

                        // Kiểm tra trùng mã
                        if (context.Nhacungcaps.Any(x => x.Mancc == txtMaNCC.Text.Trim()))
                        {
                            MessageBox.Show("Mã nhà cung cấp này đã tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var nccMoi = new Nhacungcap()
                        {
                            Mancc = txtMaNCC.Text.Trim(),
                            Tenncc = txtTenNCC.Text.Trim(),
                            Sdt = txtSdt.Text.Trim(),
                            Email = txtEmail.Text.Trim(),
                            Diachi = txtDiaChi.Text.Trim()
                        };

                        context.Nhacungcaps.Add(nccMoi);
                    }
                    else
                    {
                        // --- LOGIC CẬP NHẬT (SỬA) ---

                        // Tìm lại đối tượng trong DB để đảm bảo cập nhật cái mới nhất
                        var nccCanSua = context.Nhacungcaps.Find(_nccHienTai.Mancc);
                        if (nccCanSua != null)
                        {
                            nccCanSua.Tenncc = txtTenNCC.Text.Trim();
                            nccCanSua.Sdt = txtSdt.Text.Trim();
                            nccCanSua.Email = txtEmail.Text.Trim();
                            nccCanSua.Diachi = txtDiaChi.Text.Trim();
                        }
                    }

                    // Lưu xuống Database
                    context.SaveChanges();

                    MessageBox.Show("Lưu dữ liệu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true; // Đóng cửa sổ và báo về là thành công
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnThoat_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}