using System.Collections.Generic;

namespace PharmaDistributionApp.Models
{
    public class InvoiceEditRequestModel
    {
        public decimal TongTien { get; set; }
        public decimal VAT { get; set; }
        public string GhiChu { get; set; }
        public string TrangThai { get; set; }
        public string MaDoiTac { get; set; }
        public List<EditCartItem> ChiTiet { get; set; }
    }
}