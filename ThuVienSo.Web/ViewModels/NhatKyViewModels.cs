namespace ThuVienSo.Web.ViewModels;

/// <summary>Một dòng nhật ký hoạt động hệ thống.</summary>
public class NhatKyItem
{
    public long MaNhatKy { get; set; }
    public string? MaNguoiDung { get; set; }
    public string? HoTen { get; set; }
    public string HanhDong { get; set; } = string.Empty;
    public string? DoiTuong { get; set; }
    public string? MoTa { get; set; }
    public string? DiaChiIp { get; set; }
    public DateTime ThoiDiem { get; set; }
}

/// <summary>Bộ lọc và kết quả danh sách nhật ký hoạt động.</summary>
public class NhatKyDanhSachViewModel
{
    public string? HanhDong { get; set; }
    public string? TuKhoa { get; set; }
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }

    public List<NhatKyItem> DanhSach { get; set; } = new();
    public List<string> DanhSachHanhDong { get; set; } = new();
}
