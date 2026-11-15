namespace ThuVienSo.Web.Models;

/// <summary>Yêu cầu xin quyền đọc tài liệu số / báo cáo nghiên cứu khoa học bị giới hạn.</summary>
public class YeuCauTaiLieu
{
    public string MaYeuCau { get; set; } = string.Empty;
    public string MaDocGia { get; set; } = string.Empty;
    public string MaTaiLieuSo { get; set; } = string.Empty;
    public string? MaThuThu { get; set; }
    public DateTime NgayYeuCau { get; set; }
    public string? LyDoTruyCap { get; set; }
    public string TrangThai { get; set; } = TrangThaiYeuCau.ChoDuyet;

    public DocGia DocGia { get; set; } = null!;
    public TaiLieuSo TaiLieuSo { get; set; } = null!;
    public ThuThu? ThuThu { get; set; }
}
