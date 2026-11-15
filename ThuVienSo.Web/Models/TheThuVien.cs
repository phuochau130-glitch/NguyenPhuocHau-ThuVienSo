namespace ThuVienSo.Web.Models;

/// <summary>Thẻ thư viện cấp cho độc giả (quan hệ 1-1 với DocGia).</summary>
public class TheThuVien
{
    public string MaThe { get; set; } = string.Empty;
    public string MaDocGia { get; set; } = string.Empty;
    public DateOnly NgayCap { get; set; }

    /// <summary>Ngày bắt đầu kỳ hạn hiện tại (ngày gia hạn / cấp lại thẻ).</summary>
    public DateOnly NgayGiaHan { get; set; }

    public DateOnly NgayHetHan { get; set; }
    public string TrangThai { get; set; } = TrangThaiThe.ChoDuyet;

    /// <summary>Lệ phí cấp/gia hạn thẻ theo năm (VNĐ).</summary>
    public decimal LePhi { get; set; }

    /// <summary>Độc giả đã thanh toán lệ phí thẻ hay chưa.</summary>
    public bool DaThanhToan { get; set; }

    /// <summary>Thời điểm xác nhận thanh toán lệ phí.</summary>
    public DateTime? NgayThanhToan { get; set; }

    /// <summary>Lý do từ chối hoặc ghi chú xử lý thẻ.</summary>
    public string? GhiChu { get; set; }

    public DocGia DocGia { get; set; } = null!;

    /// <summary>Thẻ còn hiệu lực sử dụng để đăng ký mượn sách hay không.</summary>
    public bool ConHieuLuc =>
        TrangThai == TrangThaiThe.HoatDong && NgayHetHan >= DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Thẻ hết hạn (đánh dấu hết hạn hoặc còn hoạt động nhưng đã quá ngày) được phép đăng ký lại.</summary>
    public bool DaHetHan =>
        TrangThai == TrangThaiThe.HetHan ||
        (TrangThai == TrangThaiThe.HoatDong && NgayHetHan < DateOnly.FromDateTime(DateTime.Today));
}
