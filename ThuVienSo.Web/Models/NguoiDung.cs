namespace ThuVienSo.Web.Models;

/// <summary>
/// Bảng gốc lưu thông tin đăng nhập và định danh cơ bản của mọi người dùng hệ thống.
/// Mỗi người dùng có tối đa một hồ sơ tương ứng: độc giả, thủ thư hoặc quản trị viên.
/// </summary>
public class NguoiDung
{
    public string MaNguoiDung { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string MatKhau { get; set; } = string.Empty;
    public string TrangThai { get; set; } = TrangThaiNguoiDung.HoatDong;

    public DocGia? HoSoDocGia { get; set; }
    public ThuThu? HoSoThuThu { get; set; }
    public QuanTriVien? HoSoQuanTri { get; set; }

    /// <summary>Vai trò suy ra từ hồ sơ chi tiết tương ứng.</summary>
    public string VaiTro => HoSoQuanTri is not null ? Models.VaiTro.Admin
        : HoSoThuThu is not null ? Models.VaiTro.ThuThu
        : HoSoDocGia is not null ? HoSoDocGia.VaiTro
        : Models.VaiTro.SinhVien;
}

/// <summary>Độc giả (sinh viên hoặc giảng viên) sử dụng dịch vụ thư viện.</summary>
public class DocGia
{
    public string MaDocGia { get; set; } = string.Empty;
    public string MaSinhVienHoacCanBo { get; set; } = string.Empty;
    public string? Khoa { get; set; }

    /// <summary>Thời điểm được mở lại quyền đăng ký mượn; null nghĩa là không bị khóa (Quy tắc 6).</summary>
    public DateTime? KhoaMuonDen { get; set; }

    public NguoiDung NguoiDung { get; set; } = null!;
    public TheThuVien? TheThuVien { get; set; }
    public ICollection<PhieuMuon> DanhSachPhieuMuon { get; set; } = new List<PhieuMuon>();
    public ICollection<YeuCauTaiLieu> DanhSachYeuCau { get; set; } = new List<YeuCauTaiLieu>();
    public ICollection<TaiLieu> DanhSachTaiLieuDongGop { get; set; } = new List<TaiLieu>();

    /// <summary>Suy ra chức danh từ tiền tố mã số: GV = giảng viên, còn lại = sinh viên.</summary>
    public string VaiTro => MaSinhVienHoacCanBo.StartsWith("GV", StringComparison.OrdinalIgnoreCase)
        ? Models.VaiTro.GiangVien
        : Models.VaiTro.SinhVien;
}

/// <summary>Thủ thư: nhân viên quản lý kho sách, thẻ thư viện và nghiệp vụ mượn trả.</summary>
public class ThuThu
{
    public string MaThuThu { get; set; } = string.Empty;
    public string MaNhanVien { get; set; } = string.Empty;
    public string? BoPhan { get; set; }

    public NguoiDung NguoiDung { get; set; } = null!;
    public ICollection<PhieuMuon> DanhSachPhieuMuon { get; set; } = new List<PhieuMuon>();
    public ICollection<YeuCauTaiLieu> DanhSachYeuCau { get; set; } = new List<YeuCauTaiLieu>();
}

/// <summary>Quản trị viên: quản lý tài khoản, phân quyền và giám sát hệ thống.</summary>
public class QuanTriVien
{
    public string MaAdmin { get; set; } = string.Empty;

    public NguoiDung NguoiDung { get; set; } = null!;
}
