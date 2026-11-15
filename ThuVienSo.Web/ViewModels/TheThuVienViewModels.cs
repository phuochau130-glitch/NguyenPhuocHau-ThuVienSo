using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ThuVienSo.Web.Models;

namespace ThuVienSo.Web.ViewModels;

/// <summary>Bắt buộc người dùng phải tích chọn (áp dụng cho checkbox cam kết).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class PhaiTichChonAttribute : ValidationAttribute, IClientModelValidator
{
    public PhaiTichChonAttribute(string thongBao) => ErrorMessage = thongBao;

    public override bool IsValid(object? value) => value is bool daTich && daTich;

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes.TryAdd("data-val", "true");
        context.Attributes.TryAdd("data-val-phaichtichchon", ErrorMessageString);
    }
}

/// <summary>Biểu mẫu đăng ký cấp thẻ thư viện trực tuyến (UC-06).</summary>
public class DangKyTheViewModel
{
    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Display(Name = "Email liên hệ")]
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(15)]
    public string SoDienThoai { get; set; } = string.Empty;

    [Display(Name = "Mã sinh viên / cán bộ")]
    [Required(ErrorMessage = "Vui lòng nhập mã sinh viên hoặc mã cán bộ.")]
    [StringLength(30)]
    public string MaSinhVienHoacCanBo { get; set; } = string.Empty;

    [Display(Name = "Khoa / Bộ môn")]
    [StringLength(100)]
    public string? Khoa { get; set; }

    [Display(Name = "Tôi cam kết thông tin trên là chính xác và tuân thủ nội quy thư viện")]
    [PhaiTichChon("Bạn cần xác nhận cam kết trước khi gửi hồ sơ.")]
    public bool CamKet { get; set; }
}

/// <summary>Thông tin thẻ thư viện hiển thị cho độc giả theo dõi trạng thái.</summary>
public class TrangThaiTheViewModel
{
    public bool DaCoThe { get; set; }
    public TheThuVien? The { get; set; }
    public DateTime? KhoaMuonDen { get; set; }
}

/// <summary>Biểu mẫu chỉnh sửa thông tin độc giả gắn với thẻ thư viện.</summary>
public class ChinhSuaTheViewModel
{
    /// <summary>Mã thẻ đang chỉnh sửa (chỉ để hiển thị).</summary>
    public string MaThe { get; set; } = string.Empty;

    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Display(Name = "Email liên hệ")]
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(15)]
    public string SoDienThoai { get; set; } = string.Empty;

    [Display(Name = "Mã sinh viên / cán bộ")]
    [Required(ErrorMessage = "Vui lòng nhập mã sinh viên hoặc mã cán bộ.")]
    [StringLength(30)]
    public string MaSinhVienHoacCanBo { get; set; } = string.Empty;

    [Display(Name = "Khoa / Bộ môn")]
    [StringLength(100)]
    public string? Khoa { get; set; }
}
