using System.ComponentModel.DataAnnotations;

namespace ThuVienSo.Web.ViewModels;

/// <summary>Số liệu tổng hợp hiển thị trên bảng điều khiển của quản trị viên (PH-07).</summary>
public class BangDieuKhienAdminViewModel
{
    public int TongTaiKhoan { get; set; }
    public int TaiKhoanBiKhoa { get; set; }
    public int TheChoDuyet { get; set; }
    public int PhieuChoDuyet { get; set; }
    public int PhieuQuaHan { get; set; }
    public int YeuCauChoDuyet { get; set; }
    public int TaiLieuChoKiemDuyet { get; set; }

    /// <summary>Tổng số lượt đăng nhập ghi nhận trong nhật ký hoạt động.</summary>
    public int TongLuotDangNhap { get; set; }

    /// <summary>Số tài khoản đang hoạt động.</summary>
    public int TaiKhoanHoatDong { get; set; }

    /// <summary>Số liệu kho tài liệu số, lượt xem/tải và mượn trả.</summary>
    public ThongKeBangDieuKhienViewModel ThongKe { get; set; } = new();

    /// <summary>Danh sách hoạt động đăng nhập/đăng xuất gần nhất.</summary>
    public List<NhatKyItem> DanhSachDangNhap { get; set; } = new();
}

/// <summary>Bộ lọc danh sách tài khoản người dùng (UC-14).</summary>
public class DanhSachNguoiDungViewModel
{
    public string? VaiTro { get; set; }
    public string? TuKhoa { get; set; }

    /// <summary>Lọc theo trạng thái: rỗng (tất cả), "hoatdong", "khoa".</summary>
    public string? TrangThai { get; set; }

    /// <summary>Thống kê nhanh trên toàn bộ tài khoản.</summary>
    public int TongTaiKhoan { get; set; }
    public int DangHoatDong { get; set; }
    public int BiKhoa { get; set; }

    public List<NguoiDungAdminItem> DanhSach { get; set; } = new();
}

/// <summary>Một dòng tài khoản trong màn hình quản trị.</summary>
public class NguoiDungAdminItem
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
    public int SoPhieuMuon { get; set; }
    public bool CoThe { get; set; }
    public bool CoTheXoa { get; set; }
}

/// <summary>Biểu mẫu thêm mới tài khoản (UC-14).</summary>
public class ThemNguoiDungViewModel
{
    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [StringLength(15)]
    public string? SoDienThoai { get; set; }

    [Display(Name = "Vai trò")]
    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    public string VaiTro { get; set; } = Models.VaiTro.SinhVien;

    [Display(Name = "Mã sinh viên / cán bộ")]
    [StringLength(30)]
    public string? MaSinhVienHoacCanBo { get; set; }

    [Display(Name = "Mã nhân viên")]
    [StringLength(30)]
    public string? MaNhanVien { get; set; }

    [Display(Name = "Khoa / Bộ môn")]
    [StringLength(100)]
    public string? Khoa { get; set; }

    [Display(Name = "Bộ phận")]
    [StringLength(100)]
    public string? BoPhan { get; set; }

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = string.Empty;
}

/// <summary>Biểu mẫu cập nhật tài khoản (UC-14).</summary>
public class SuaNguoiDungViewModel
{
    public string MaNguoiDung { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;

    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [StringLength(15)]
    public string? SoDienThoai { get; set; }

    [Display(Name = "Mã sinh viên / cán bộ")]
    [StringLength(30)]
    public string? MaSinhVienHoacCanBo { get; set; }

    [Display(Name = "Mã nhân viên")]
    [StringLength(30)]
    public string? MaNhanVien { get; set; }

    [Display(Name = "Khoa / Bộ môn")]
    [StringLength(100)]
    public string? Khoa { get; set; }

    [Display(Name = "Bộ phận")]
    [StringLength(100)]
    public string? BoPhan { get; set; }
}

/// <summary>Biểu mẫu đặt lại mật khẩu tài khoản (UC-14).</summary>
public class DatLaiMatKhauViewModel
{
    public string MaNguoiDung { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự.")]
    [DataType(DataType.Password)]
    public string MatKhauMoi { get; set; } = string.Empty;
}
