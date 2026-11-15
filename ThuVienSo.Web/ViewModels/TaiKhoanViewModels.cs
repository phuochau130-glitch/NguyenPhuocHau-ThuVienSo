using System.ComponentModel.DataAnnotations;
using ThuVienSo.Web.Models;

namespace ThuVienSo.Web.ViewModels;

/// <summary>Biểu mẫu đăng nhập.</summary>
public class DangNhapViewModel
{
    [Display(Name = "Email")]
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool GhiNho { get; set; }

    public string? ReturnUrl { get; set; }
}

/// <summary>Biểu mẫu đổi mật khẩu.</summary>
public class DoiMatKhauViewModel
{
    [Display(Name = "Mật khẩu hiện tại")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    public string MatKhauCu { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [DataType(DataType.Password)]
    public string MatKhauMoi { get; set; } = string.Empty;

    [Display(Name = "Xác nhận mật khẩu mới")]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    public string XacNhanMatKhauMoi { get; set; } = string.Empty;
}

/// <summary>Thông tin hồ sơ cá nhân hiển thị ở trang tài khoản.</summary>
public class HoSoCaNhanViewModel
{
    public string MaNguoiDung { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string VaiTro { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string? MaSinhVienHoacCanBo { get; set; }
    public string? Khoa { get; set; }
    public string? MaNhanVien { get; set; }
    public string? BoPhan { get; set; }
    public DateTime? KhoaMuonDen { get; set; }
}

/// <summary>Thông tin liên hệ được phép tự cập nhật trong hồ sơ cá nhân.</summary>
public class CapNhatHoSoViewModel
{
    public string VaiTro { get; set; } = string.Empty;

    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
    public string HoTen { get; set; } = string.Empty;

    [Display(Name = "Email đăng nhập")]
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự.")]
    public string? SoDienThoai { get; set; }

    [Display(Name = "Khoa / Bộ môn")]
    [StringLength(100, ErrorMessage = "Tên khoa/bộ môn không được vượt quá 100 ký tự.")]
    public string? Khoa { get; set; }

    [Display(Name = "Bộ phận")]
    [StringLength(100, ErrorMessage = "Tên bộ phận không được vượt quá 100 ký tự.")]
    public string? BoPhan { get; set; }
}

/// <summary>Lịch sử đọc của độc giả: phiếu mượn trả và yêu cầu đọc tài liệu số.</summary>
public class LichSuDocViewModel
{
    public List<PhieuMuon> PhieuMuon { get; set; } = new();
    public List<YeuCauTaiLieu> YeuCau { get; set; } = new();
    public List<TaiLieuDaDocViewModel> TaiLieuDaDoc { get; set; } = new();
}

/// <summary>Một tài liệu người dùng đã mở, kèm thời điểm đọc gần nhất để đọc tiếp.</summary>
public class TaiLieuDaDocViewModel
{
    public TaiLieu TaiLieu { get; set; } = null!;
    public DateTime LanDocGanNhat { get; set; }
}
