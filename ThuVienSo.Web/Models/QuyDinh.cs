namespace ThuVienSo.Web.Models;

/// <summary>
/// Các vai trò (tác nhân) trong hệ thống.
/// </summary>
public static class VaiTro
{
    public const string SinhVien = "SinhVien";
    public const string GiangVien = "GiangVien";
    public const string ThuThu = "ThuThu";
    public const string Admin = "Admin";

    /// <summary>Chuỗi vai trò dùng cho [Authorize(Roles = ...)] của nhóm độc giả.</summary>
    public const string DocGia = SinhVien + "," + GiangVien;

    public static string TenHienThi(string vaiTro) => vaiTro switch
    {
        SinhVien => "Sinh viên",
        GiangVien => "Giảng viên",
        ThuThu => "Thủ thư",
        Admin => "Quản trị viên",
        _ => "Người dùng"
    };
}

/// <summary>Trạng thái tài khoản người dùng (NguoiDung.trangThai).</summary>
public static class TrangThaiNguoiDung
{
    public const string HoatDong = "Hoạt động";
    public const string BiKhoa = "Bị khóa";
}

/// <summary>Trạng thái thẻ thư viện (TheThuVien.trangThai).</summary>
public static class TrangThaiThe
{
    public const string ChoDuyet = "Chờ duyệt";
    public const string HoatDong = "Hoạt động";
    public const string HetHan = "Hết hạn";
    public const string BiKhoa = "Bị khóa";
    public const string TuChoi = "Từ chối";
}

/// <summary>Trạng thái phiếu mượn (PhieuMuon.trangThai).</summary>
public static class TrangThaiPhieuMuon
{
    public const string ChoDuyet = "Chờ duyệt";
    public const string DangGiu = "Đang giữ";
    public const string DangMuon = "Đang mượn";
    public const string DaTra = "Đã trả";
    public const string DaHuy = "Đã hủy";
    public const string TuChoi = "Từ chối";

    /// <summary>Quá hạn giữ sách mà độc giả không đến nhận bản cứng.</summary>
    public const string ChuaNhan = "Chưa nhận sách";
}

/// <summary>Trạng thái yêu cầu xin quyền đọc tài liệu (YeuCauTaiLieu.trangThai).</summary>
public static class TrangThaiYeuCau
{
    public const string ChoDuyet = "Chờ duyệt";
    public const string DaPheDuyet = "Đã phê duyệt";
    public const string TuChoi = "Từ chối";
}

/// <summary>Trạng thái kiểm duyệt tài liệu đóng góp (TaiLieu.trangThaiDuyet).</summary>
public static class TrangThaiDuyetTaiLieu
{
    public const string ChoDuyet = "Chờ duyệt";
    public const string DaDuyet = "Đã duyệt";
    public const string TuChoi = "Từ chối";
}

/// <summary>
/// Các tham số quy định nghiệp vụ của thư viện (Quy tắc 5, 6, 7 trong tài liệu phân tích).
/// </summary>
public static class QuyDinh
{
    /// <summary>Thời hạn giữ sách chờ độc giả đến nhận (7 ngày).</summary>
    public const int SoNgayGiuSach = 7;

    /// <summary>Thời hạn khóa quyền đăng ký mượn khi không đến nhận sách (2 ngày).</summary>
    public const int SoNgayKhoaMuon = 2;

    /// <summary>Thời hạn mượn sách thực tế sau khi nhận sách.</summary>
    public const int SoNgayMuon = 14;

    /// <summary>Thời hạn mượn tối thiểu độc giả được chọn khi đăng ký mượn.</summary>
    public const int SoNgayMuonToiThieu = 7;

    /// <summary>Thời hạn mượn tối đa độc giả được chọn khi đăng ký mượn.</summary>
    public const int SoNgayMuonToiDa = 30;

    /// <summary>Lệ phí cấp/gia hạn thẻ thư viện theo năm (VNĐ).</summary>
    public const decimal LePhiTheThuVien = 40000m;

    /// <summary>Mức phạt mỗi ngày trả trễ (VNĐ).</summary>
    public const decimal PhatMoiNgay = 5000m;

    /// <summary>Số ký tự tối đa cho phần xem trước sách vật lý.</summary>
    public const int SoKyTuXemTruoc = 1500;

    /// <summary>
    /// Tỉ lệ nội dung tệp đọc thử (PDF/Word) của sách vật lý được phép xem.
    /// Khóa 1 nửa nội dung để người dùng phải đăng ký mượn bản cứng mới đọc được toàn văn.
    /// </summary>
    public const double TyLeKhoaXemTruoc = 0.5;

    /// <summary>Thư mục lưu tệp tài liệu số tải lên.</summary>
    public const string ThuMucTaiLieu = "uploads/tailieu";
}
