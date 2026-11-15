namespace ThuVienSo.Web.Models;

/// <summary>Phiếu mượn sách vật lý: ghi nhận yêu cầu, giữ sách, giao sách và trả sách.</summary>
public class PhieuMuon
{
    public string MaPhieu { get; set; } = string.Empty;
    public string MaDocGia { get; set; } = string.Empty;
    public string MaSach { get; set; } = string.Empty;
    public string? MaThuThu { get; set; }

    /// <summary>Thời điểm lập phiếu / bắt đầu giữ sách.</summary>
    public DateTime NgayMuon { get; set; }

    public DateOnly NgayPhaiTra { get; set; }
    public DateOnly? NgayTraThucTe { get; set; }
    public string TrangThai { get; set; } = TrangThaiPhieuMuon.ChoDuyet;

    /// <summary>Thời hạn mượn (số ngày) độc giả chọn khi đăng ký.</summary>
    public int SoNgayMuon { get; set; } = QuyDinh.SoNgayMuon;

    /// <summary>Tiền phạt do trả sách trễ hạn (VNĐ).</summary>
    public decimal TienPhatTraTre { get; set; }

    /// <summary>Tiền phạt do hư hỏng sách (VNĐ).</summary>
    public decimal TienPhatHuHong { get; set; }

    /// <summary>Tổng tiền phạt của phiếu = phạt trả trễ + phạt hư hỏng.</summary>
    public decimal TienPhat => TienPhatTraTre + TienPhatHuHong;

    public DocGia DocGia { get; set; } = null!;
    public SachVatLi Sach { get; set; } = null!;
    public ThuThu? ThuThu { get; set; }

    /// <summary>Hạn cuối độc giả phải đến nhận sách (Quy tắc 5).</summary>
    public DateTime HanNhanSach => NgayMuon.AddDays(Models.QuyDinh.SoNgayGiuSach);
}
