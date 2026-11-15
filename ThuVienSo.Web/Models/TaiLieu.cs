namespace ThuVienSo.Web.Models;

/// <summary>
/// Lớp gốc quản lý thông tin chung của mọi ấn phẩm trong thư viện.
/// </summary>
public class TaiLieu
{
    public string MaTaiLieu { get; set; } = string.Empty;
    public string TenTaiLieu { get; set; } = string.Empty;
    public string? TacGia { get; set; }
    public string? NhaXuatBan { get; set; }
    public int? NamXuatBan { get; set; }
    public string? DanhMuc { get; set; }

    /// <summary>0: truy cập tự do, 1: tài liệu bị giới hạn truy cập.</summary>
    public bool LaTaiLieuHanChe { get; set; }

    /// <summary>Tổng số lượt xem/đọc tài liệu (gồm cả sách vật lý và tài liệu số).</summary>
    public int SoLuotXem { get; set; }

    /// <summary>Đường dẫn ảnh bìa (trang bìa) của tài liệu, dùng để làm nổi bật sách.</summary>
    public string? DuongDanAnhBia { get; set; }

    /// <summary>Ghi chú / lý do từ chối kiểm duyệt tài liệu nghiên cứu khoa học.</summary>
    public string? GhiChu { get; set; }

    /// <summary>Trạng thái kiểm duyệt tài liệu đóng góp (Quy tắc 8).</summary>
    public string TrangThaiDuyet { get; set; } = TrangThaiDuyetTaiLieu.DaDuyet;

    /// <summary>Độc giả đã đóng góp tài liệu nghiên cứu khoa học (nếu có).</summary>
    public string? MaNguoiDang { get; set; }

    public DocGia? NguoiDang { get; set; }

    /// <summary>Thông tin sách bản cứng (nếu là sách vật lý).</summary>
    public SachVatLi? BanSach { get; set; }

    /// <summary>Thông tin tệp tài liệu điện tử (nếu là tài liệu số).</summary>
    public TaiLieuSo? BanSo { get; set; }

    /// <summary>Loại tài liệu suy ra từ bản chi tiết tương ứng.</summary>
    public string LoaiTaiLieu => BanSach is not null ? "Sách vật lý"
        : BanSo is not null ? "Tài liệu số"
        : "Khác";
}

/// <summary>Một tài liệu được độc giả lưu vào tủ sách cá nhân.</summary>
public class TaiLieuTuSach
{
    public string MaDocGia { get; set; } = string.Empty;
    public string MaTaiLieu { get; set; } = string.Empty;
    public DateTime NgayLuu { get; set; }

    public DocGia DocGia { get; set; } = null!;
    public TaiLieu TaiLieu { get; set; } = null!;
}

/// <summary>Sách bản cứng lưu kho tại thư viện.</summary>
public class SachVatLi
{
    public string MaSach { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? MaVach { get; set; }
    public string? ViTriKeSach { get; set; }
    public int TongSoLuong { get; set; }
    public int SoLuongKhaDung { get; set; }

    /// <summary>Đường dẫn tệp PDF/Word cho phép đọc thử nội dung trước khi mượn bản cứng.</summary>
    public string? DuongDanTepXemTruoc { get; set; }

    /// <summary>Định dạng tệp đọc thử (PDF, DOCX...).</summary>
    public string? DinhDangTepXemTruoc { get; set; }

    public TaiLieu TaiLieu { get; set; } = null!;
    public ICollection<PhieuMuon> DanhSachPhieuMuon { get; set; } = new List<PhieuMuon>();
}

/// <summary>Tệp tài liệu trực tuyến (PDF, EPUB...).</summary>
public class TaiLieuSo
{
    public string MaTaiLieuSo { get; set; } = string.Empty;
    public string DuongDanFile { get; set; } = string.Empty;
    public double? DungLuongFile { get; set; }
    public string? DinhDang { get; set; }
    public int SoLuotTai { get; set; }

    public TaiLieu TaiLieu { get; set; } = null!;
    public ICollection<YeuCauTaiLieu> DanhSachYeuCau { get; set; } = new List<YeuCauTaiLieu>();
}
