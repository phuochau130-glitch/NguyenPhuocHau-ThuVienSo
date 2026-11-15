using System.ComponentModel.DataAnnotations;
using ThuVienSo.Web.Models;

namespace ThuVienSo.Web.ViewModels;

/// <summary>Số liệu tổng hợp hiển thị trên bảng điều khiển của thủ thư (PH-02).</summary>
public class BangDieuKhienThuThuViewModel
{
    /// <summary>Số hồ sơ thẻ thư viện đang chờ xét duyệt (PH-05).</summary>
    public int TheChoDuyet { get; set; }

    /// <summary>Số phiếu mượn đang chờ duyệt (PH-06).</summary>
    public int PhieuChoDuyet { get; set; }

    /// <summary>Số phiếu đang giữ sách chờ độc giả đến nhận.</summary>
    public int PhieuDangGiu { get; set; }

    /// <summary>Số phiếu độc giả đang mượn sách.</summary>
    public int PhieuDangMuon { get; set; }

    /// <summary>Số phiếu mượn đã quá hạn trả.</summary>
    public int PhieuQuaHan { get; set; }

    /// <summary>Số yêu cầu xin quyền đọc tài liệu đang chờ duyệt (PH-04).</summary>
    public int YeuCauChoDuyet { get; set; }

    /// <summary>Số tài liệu nghiên cứu khoa học đang chờ kiểm duyệt (Quy tắc 8).</summary>
    public int TaiLieuChoKiemDuyet { get; set; }

    /// <summary>Tổng số việc cần xử lý.</summary>
    public int TongViecCanXuLy =>
        TheChoDuyet + PhieuChoDuyet + YeuCauChoDuyet + TaiLieuChoKiemDuyet;

    /// <summary>Số liệu thống kê lượt đọc/mượn phục vụ bảng điều khiển.</summary>
    public ThongKeBangDieuKhienViewModel ThongKe { get; set; } = new();
}

/// <summary>Biểu mẫu thêm / sửa sách vật lý trong kho của thủ thư.</summary>
public class SachVatLiFormViewModel
{
    public string? MaSach { get; set; }

    /// <summary>Số bản khả dụng hiện tại (chỉ hiển thị khi sửa).</summary>
    public int? SoLuongKhaDungHienTai { get; set; }

    [Display(Name = "Tên sách")]
    [Required(ErrorMessage = "Vui lòng nhập tên sách.")]
    [StringLength(255)]
    public string TenTaiLieu { get; set; } = string.Empty;

    [Display(Name = "Tác giả")]
    [StringLength(200)]
    public string? TacGia { get; set; }
    public List<string> DanhSachTacGia { get; set; } = new();

    [Display(Name = "Nhà xuất bản")]
    [StringLength(150)]
    public string? NhaXuatBan { get; set; }

    [Display(Name = "Năm xuất bản")]
    [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ.")]
    public int? NamXuatBan { get; set; }

    [Display(Name = "Danh mục")]
    [StringLength(100)]
    public string? DanhMuc { get; set; }
    public List<string> DanhSachDanhMuc { get; set; } = new();

    [Display(Name = "Tổng số lượng")]
    [Range(0, 10000, ErrorMessage = "Tổng số lượng phải từ 0 đến 10000.")]
    public int TongSoLuong { get; set; }

    [Display(Name = "Vị trí kệ sách")]
    [StringLength(100, ErrorMessage = "Vị trí kệ sách không được vượt quá 100 ký tự.")]
    public string? ViTriKeSach { get; set; }

    [Display(Name = "Tệp đọc thử (PDF/Word)")]
    public IFormFile? TepXemTruoc { get; set; }

    /// <summary>Định dạng tệp đọc thử hiện tại (chỉ hiển thị khi sửa).</summary>
    public string? DinhDangTepXemTruocHienTai { get; set; }

    /// <summary>Sách đã có tệp đọc thử hay chưa.</summary>
    public bool CoTepXemTruoc { get; set; }

    [Display(Name = "Ảnh bìa (trang bìa)")]
    public IFormFile? AnhBia { get; set; }

    /// <summary>Đường dẫn ảnh bìa hiện tại (chỉ hiển thị khi sửa).</summary>
    public string? DuongDanAnhBiaHienTai { get; set; }

    /// <summary>Sách đã có ảnh bìa hay chưa.</summary>
    public bool CoAnhBia { get; set; }

    [Display(Name = "Ghi chú")]
    [StringLength(500)]
    public string? GhiChu { get; set; }
}

/// <summary>Biểu mẫu thêm / sửa tài liệu số của thủ thư.</summary>
public class TaiLieuSoFormViewModel
{
    public string? MaTaiLieuSo { get; set; }

    /// <summary>Thông tin tệp hiện tại (chỉ hiển thị khi sửa).</summary>
    public string? DinhDangHienTai { get; set; }
    public double? DungLuongFileHienTai { get; set; }

    [Display(Name = "Tên tài liệu")]
    [Required(ErrorMessage = "Vui lòng nhập tên tài liệu.")]
    [StringLength(255)]
    public string TenTaiLieu { get; set; } = string.Empty;

    [Display(Name = "Tác giả")]
    [StringLength(200)]
    public string? TacGia { get; set; }
    public List<string> DanhSachTacGia { get; set; } = new();

    [Display(Name = "Nhà xuất bản")]
    [StringLength(150)]
    public string? NhaXuatBan { get; set; }

    [Display(Name = "Năm xuất bản")]
    [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ.")]
    public int? NamXuatBan { get; set; }

    [Display(Name = "Danh mục")]
    [StringLength(100)]
    public string? DanhMuc { get; set; }
    public List<string> DanhSachDanhMuc { get; set; } = new();

    [Display(Name = "Tài liệu giới hạn truy cập (độc giả phải xin quyền đọc)")]
    public bool LaTaiLieuHanChe { get; set; }

    [Display(Name = "Tệp tài liệu (PDF, EPUB, DOC, DOCX, TXT - tối đa 25MB)")]
    public IFormFile? TepTaiLieu { get; set; }

    [Display(Name = "Ảnh bìa (trang bìa)")]
    public IFormFile? AnhBia { get; set; }

    /// <summary>Đường dẫn ảnh bìa hiện tại (chỉ hiển thị khi sửa).</summary>
    public string? DuongDanAnhBiaHienTai { get; set; }

    /// <summary>Tài liệu đã có ảnh bìa hay chưa.</summary>
    public bool CoAnhBia { get; set; }

    [Display(Name = "Ghi chú")]
    [StringLength(500)]
    public string? GhiChu { get; set; }
}

/// <summary>Dữ liệu hiển thị và nhập mới cho màn quản lý danh mục, tác giả.</summary>
public class QuanLyDanhMucTacGiaViewModel
{
    public List<DanhMucTaiLieu> DanhMuc { get; set; } = new();
    public List<TacGiaTaiLieu> TacGia { get; set; } = new();
}
