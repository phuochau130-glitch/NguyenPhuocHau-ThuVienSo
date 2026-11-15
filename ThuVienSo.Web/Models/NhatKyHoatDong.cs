namespace ThuVienSo.Web.Models;

/// <summary>
/// Nhật ký hoạt động hệ thống: ghi nhận đăng nhập, xem/tải tài liệu, mượn trả và thao tác quản trị.
/// </summary>
public class NhatKyHoatDong
{
    public long MaNhatKy { get; set; }

    /// <summary>Mã người dùng phát sinh hoạt động (có thể rỗng với hoạt động ẩn danh).</summary>
    public string? MaNguoiDung { get; set; }

    /// <summary>Loại hành động: Đăng nhập, Đăng xuất, Xem tài liệu, Tải tài liệu, Mượn sách, Trả sách, Phạt, Quản trị...</summary>
    public string HanhDong { get; set; } = string.Empty;

    /// <summary>Đối tượng bị tác động (mã tài liệu, mã phiếu mượn, mã tài khoản...).</summary>
    public string? DoiTuong { get; set; }

    /// <summary>Mô tả chi tiết hoạt động.</summary>
    public string? MoTa { get; set; }

    /// <summary>Địa chỉ IP của người thực hiện (nếu xác định được).</summary>
    public string? DiaChiIp { get; set; }

    public DateTime ThoiDiem { get; set; }

    public NguoiDung? NguoiDung { get; set; }
}

/// <summary>Các nhãn hành động dùng thống nhất khi ghi nhật ký hoạt động.</summary>
public static class HanhDongNhatKy
{
    public const string DangNhap = "Đăng nhập";
    public const string DangXuat = "Đăng xuất";
    public const string XemTaiLieu = "Xem tài liệu";
    public const string TaiTaiLieu = "Tải tài liệu";
    public const string MuonSach = "Mượn sách";
    public const string TraSach = "Trả sách";
    public const string PhatTien = "Phạt tiền";
    public const string QuanTri = "Quản trị";
    public const string DoiMatKhau = "Đổi mật khẩu";
    public const string CapNhatHoSo = "Cập nhật thông tin người dùng";
}
