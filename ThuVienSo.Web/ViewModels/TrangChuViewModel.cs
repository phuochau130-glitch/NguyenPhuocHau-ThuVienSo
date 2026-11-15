using ThuVienSo.Web.Models;

namespace ThuVienSo.Web.ViewModels;

/// <summary>Dữ liệu hiển thị trang chủ: danh mục và danh sách tài liệu nổi bật.</summary>
public class TrangChuViewModel
{
    public List<string> DanhSachDanhMuc { get; set; } = new();

    /// <summary>Tài liệu nổi bật, sắp xếp theo lượt xem giảm dần.</summary>
    public List<TaiLieu> TaiLieuNoiBat { get; set; } = new();
}
