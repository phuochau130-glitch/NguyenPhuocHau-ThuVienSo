namespace ThuVienSo.Web.Models;

/// <summary>Danh mục dùng chung để phân loại sách và tài liệu.</summary>
public class DanhMucTaiLieu
{
    public int MaDanhMuc { get; set; }
    public string TenDanhMuc { get; set; } = string.Empty;
}

/// <summary>Tác giả dùng chung cho sách và tài liệu số.</summary>
public class TacGiaTaiLieu
{
    public int MaTacGia { get; set; }
    public string TenTacGia { get; set; } = string.Empty;
}
