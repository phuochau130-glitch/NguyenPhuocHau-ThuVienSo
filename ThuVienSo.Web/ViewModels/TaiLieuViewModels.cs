using System.ComponentModel.DataAnnotations;
using ThuVienSo.Web.Models;

namespace ThuVienSo.Web.ViewModels;

/// <summary>Bộ lọc và kết quả tra cứu tài liệu (UC-03).</summary>
public class TraCuuTaiLieuViewModel
{
    public string? TuKhoa { get; set; }
    public string? DanhMuc { get; set; }
    public string? Loai { get; set; }
    public bool ChiConKhaDung { get; set; }

    public List<TaiLieu> KetQua { get; set; } = new();
    public List<string> DanhSachDanhMuc { get; set; } = new();

    public bool DaTraCuu { get; set; }
}

/// <summary>Chi tiết tài liệu kèm thông tin quyền truy cập của độc giả đang đăng nhập.</summary>
public class ChiTietTaiLieuViewModel
{
    public TaiLieu TaiLieu { get; set; } = null!;

    /// <summary>Độc giả hiện đang có quyền đọc toàn văn tài liệu giới hạn hay không.</summary>
    public bool DaCoQuyenDoc { get; set; }

    /// <summary>Yêu cầu xin quyền gần nhất của độc giả cho tài liệu này (nếu có).</summary>
    public YeuCauTaiLieu? YeuCauGanNhat { get; set; }

    /// <summary>Thẻ thư viện hợp lệ của độc giả (nếu có).</summary>
    public bool CoTheThuVienHopLe { get; set; }

    public string? TrangThaiThe { get; set; }

    public bool DaHetHanThe { get; set; }

    public DateOnly? NgayHetHanThe { get; set; }

    public string? GhiChuThe { get; set; }

    /// <summary>Độc giả đang bị khóa quyền đăng ký mượn (Quy tắc 6).</summary>
    public bool DangBiKhoaMuon { get; set; }

    public DateTime? KhoaMuonDen { get; set; }

    public bool DaDangNhap { get; set; }
}

/// <summary>Nội dung xem trước sách vật lý (Quy tắc 3).</summary>
public class XemTruocSachViewModel
{
    public TaiLieu TaiLieu { get; set; } = null!;
    public SachVatLi Sach { get; set; } = null!;
    public string NoiDungXemTruoc { get; set; } = string.Empty;
    public bool BiCatNgan { get; set; }

    /// <summary>URL tệp PDF/Word đọc thử (nếu thủ thư đã tải lên).</summary>
    public string? UrlTepXemTruoc { get; set; }

    /// <summary>Định dạng tệp đọc thử: PDF, DOCX...</summary>
    public string? DinhDangTepXemTruoc { get; set; }
}

/// <summary>Biểu mẫu đóng góp tài liệu nghiên cứu khoa học (UC-05 - Quy tắc 8).</summary>
public class TaiLenNckhViewModel
{
    [Display(Name = "Tên bài báo / báo cáo")]
    [Required(ErrorMessage = "Vui lòng nhập tên tài liệu.")]
    [StringLength(255)]
    public string TenTaiLieu { get; set; } = string.Empty;

    [Display(Name = "Tác giả")]
    [StringLength(200)]
    public string? TacGia { get; set; }

    [Display(Name = "Lĩnh vực / danh mục")]
    [StringLength(100)]
    public string? DanhMuc { get; set; }

    [Display(Name = "Nhà xuất bản / tạp chí")]
    [StringLength(150)]
    public string? NhaXuatBan { get; set; }

    [Display(Name = "Năm công bố")]
    [Range(1900, 2100, ErrorMessage = "Năm công bố không hợp lệ.")]
    public int? NamXuatBan { get; set; }

    [Display(Name = "Tóm tắt nội dung")]
    [StringLength(2000)]
    public string? TomTat { get; set; }

    [Display(Name = "Tệp tài liệu (PDF, EPUB)")]
    [Required(ErrorMessage = "Vui lòng chọn tệp tài liệu.")]
    public IFormFile? TepTaiLieu { get; set; }

    [Display(Name = "Tài liệu bị giới hạn truy cập")]
    public bool LaTaiLieuHanChe { get; set; } = true;
}

/// <summary>Một dòng trong danh sách tài liệu đã đóng góp của độc giả.</summary>
public class TaiLieuDongGopViewModel
{
    public TaiLieu TaiLieu { get; set; } = null!;
    public TaiLieuSo? BanSo { get; set; }
}

/// <summary>Dữ liệu hiển thị trên trang đọc tài liệu số trực tuyến.</summary>
public class DocTaiLieuViewModel
{
    public TaiLieu TaiLieu { get; set; } = null!;
    public TaiLieuSo BanSo { get; set; } = null!;

    /// <summary>URL truy cập tệp tài liệu để nhúng vào trình xem.</summary>
    public string UrlTaiNguyen { get; set; } = string.Empty;

    /// <summary>Độc giả hiện tại đã có quyền đọc toàn văn tài liệu này chưa.</summary>
    public bool DaCoQuyenDoc { get; set; }
}
