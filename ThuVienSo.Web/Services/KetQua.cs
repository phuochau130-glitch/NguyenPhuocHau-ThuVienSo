using System.Security.Claims;

namespace ThuVienSo.Web.Services;

/// <summary>Kết quả trả về của một thao tác nghiệp vụ (thành công/thất bại kèm thông báo).</summary>
public readonly record struct KetQua(bool ThanhCong, string ThongBao)
{
    public static KetQua Ok(string thongBao) => new(true, thongBao);
    public static KetQua Loi(string thongBao) => new(false, thongBao);
}

/// <summary>Các tiện ích đọc thông tin người dùng đang đăng nhập từ ClaimsPrincipal.</summary>
public static class NguoiDungHienTai
{
    public const string LoaiClaimMaNguoiDung = "maNguoiDung";
    public const string LoaiClaimMaVaiTro = "maVaiTro";

    /// <summary>Mã người dùng (maNguoiDung) của phiên đăng nhập hiện tại.</summary>
    public static string? MaNguoiDung(this ClaimsPrincipal? user) =>
        user?.FindFirst(LoaiClaimMaNguoiDung)?.Value;

    /// <summary>Mã hồ sơ theo vai trò: mã độc giả, mã thủ thư hoặc mã admin.</summary>
    public static string? MaVaiTro(this ClaimsPrincipal? user) =>
        user?.FindFirst(LoaiClaimMaVaiTro)?.Value;

    /// <summary>Tên hiển thị của người dùng đang đăng nhập.</summary>
    public static string TenHienThi(this ClaimsPrincipal? user) =>
        user?.Identity?.Name ?? string.Empty;

    /// <summary>Vai trò hiện tại (SinhVien, GiangVien, ThuThu, Admin).</summary>
    public static string VaiTro(this ClaimsPrincipal? user) =>
        user?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
}
