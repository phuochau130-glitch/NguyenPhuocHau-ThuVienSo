using ThuVienSo.Web.Models;

namespace ThuVienSo.Web.ViewModels;

/// <summary>Số liệu thống kê tài liệu (UC-15 - Include: Thống kê số lượng tài liệu).</summary>
public class ThongKeTaiLieuViewModel
{
    public int TongSoTaiLieu { get; set; }
    public int SoSachVatLi { get; set; }
    public int SoTaiLieuSo { get; set; }
    public int SoTaiLieuGioiHan { get; set; }
    public int SoTaiLieuKhongGioiHan => TongSoTaiLieu - SoTaiLieuGioiHan;
    public int TongSoBanSach { get; set; }
    public int TongSoBanKhaDung { get; set; }

    /// <summary>Tổng lượt xem/đọc của mọi tài liệu (số và vật lý).</summary>
    public int TongLuotXem { get; set; }

    /// <summary>Lượt xem tài liệu số.</summary>
    public int LuotXemTaiLieuSo { get; set; }

    /// <summary>Lượt xem sách vật lý.</summary>
    public int LuotXemSachVatLi { get; set; }

    /// <summary>Tổng lượt tải tài liệu số.</summary>
    public int TongLuotTai { get; set; }

    /// <summary>Tổng lượt mượn sách vật lý (số phiếu mượn phát sinh).</summary>
    public int TongLuotMuon { get; set; }

    public int SoTaiLieuChoDuyet { get; set; }

    public List<ThongKeTheoDanhMuc> TheoDanhMuc { get; set; } = new();

    /// <summary>Danh sách tài liệu được xem/đọc nhiều nhất.</summary>
    public List<ThongKeTaiLieuXemNhieu> TaiLieuXemNhieuNhat { get; set; } = new();
}

/// <summary>Số liệu thống kê hoạt động mượn trả (UC-15 - Include: Thống kê hoạt động mượn trả).</summary>
public class ThongKeMuonTraViewModel
{
    public int TongLuotMuon { get; set; }
    public int DangMuon { get; set; }
    public int DangGiu { get; set; }
    public int ChoDuyet { get; set; }
    public int DaTra { get; set; }
    public int QuaHan { get; set; }
    public int SoLuotQuaHanDaTra { get; set; }

    /// <summary>Tổng tiền phạt do trả sách trễ.</summary>
    public decimal TienPhatTraTre { get; set; }

    /// <summary>Tổng tiền phạt do hư hỏng sách.</summary>
    public decimal TienPhatHuHong { get; set; }

    public decimal TongTienPhat => TienPhatTraTre + TienPhatHuHong;

    public List<ThongKeSachMuonNhieu> SachMuonNhieuNhat { get; set; } = new();
    public List<PhieuMuon> ChiTietPhieuMuon { get; set; } = new();

    /// <summary>Danh sách các khoản tiền phạt kèm lý do (hư hỏng / trả trễ).</summary>
    public List<ChiTietPhat> DanhSachPhat { get; set; } = new();
}

/// <summary>Một dòng thống kê nhóm theo danh mục tài liệu.</summary>
public class ThongKeTheoDanhMuc
{
    public string DanhMuc { get; set; } = string.Empty;
    public int SoLuong { get; set; }
}

/// <summary>Một dòng thống kê sách được mượn nhiều nhất.</summary>
public class ThongKeSachMuonNhieu
{
    public string TenSach { get; set; } = string.Empty;
    public int SoLuotMuon { get; set; }
}

/// <summary>Một dòng thống kê tài liệu được xem/đọc nhiều nhất.</summary>
public class ThongKeTaiLieuXemNhieu
{
    public string MaTaiLieu { get; set; } = string.Empty;
    public string TenTaiLieu { get; set; } = string.Empty;
    public string LoaiTaiLieu { get; set; } = string.Empty;
    public int SoLuotXem { get; set; }
}

/// <summary>Một khoản tiền phạt của phiếu mượn kèm lý do.</summary>
public class ChiTietPhat
{
    public string MaPhieu { get; set; } = string.Empty;
    public string? MaNguoiDung { get; set; }
    public string TenDocGia { get; set; } = string.Empty;
    public string TenSach { get; set; } = string.Empty;
    public decimal TienPhatTraTre { get; set; }
    public decimal TienPhatHuHong { get; set; }
    public decimal TongTien => TienPhatTraTre + TienPhatHuHong;
    public string LyDo { get; set; } = string.Empty;
    public DateTime? NgayTra { get; set; }
}

/// <summary>Một lượt tải/đọc tài liệu (trích từ nhật ký hoạt động).</summary>
public class ChiTietTaiDoc
{
    public DateTime ThoiDiem { get; set; }
    public string? MaNguoiDung { get; set; }
    public string? TenNguoiDung { get; set; }
    public string MaTaiLieu { get; set; } = string.Empty;
    public string TenTaiLieu { get; set; } = string.Empty;
    public string HanhDong { get; set; } = string.Empty;
}

/// <summary>Một lần đăng nhập hoặc thay đổi thông tin tài khoản được ghi nhận.</summary>
public class ChiTietHoatDongTaiKhoan
{
    public DateTime ThoiDiem { get; set; }
    public string? MaNguoiThucHien { get; set; }
    public string? TenNguoiThucHien { get; set; }
    public string? MaTaiKhoanTacDong { get; set; }
    public string HanhDong { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? DiaChiIp { get; set; }
}

/// <summary>Tổng hợp truy cập và thay đổi tài khoản trong khoảng báo cáo.</summary>
public class ThongKeHoatDongTaiKhoanViewModel
{
    public int TongLuotTruyCap { get; set; }
    public int SoTaiKhoanTruyCap { get; set; }
    public int SoLanDoiMatKhau { get; set; }
    public int SoLanCapNhatHoSo { get; set; }
    public List<ChiTietHoatDongTaiKhoan> DanhSach { get; set; } = new();
}

/// <summary>Một dòng thống kê tài liệu theo lượt xem, mượn và trả.</summary>
public class ThongKeTaiLieuTheoLuot
{
    public string MaTaiLieu { get; set; } = string.Empty;
    public string TenTaiLieu { get; set; } = string.Empty;
    public string LoaiTaiLieu { get; set; } = string.Empty;
    public int LuotXem { get; set; }
    public int LuotMuon { get; set; }
    public int LuotTra { get; set; }
}

/// <summary>Một dòng thống kê lượt phạt và số tiền phạt theo từng tài khoản.</summary>
public class ThongKePhatTheoTaiKhoan
{
    public string MaNguoiDung { get; set; } = string.Empty;
    public string TenDocGia { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int SoLuotPhat { get; set; }
    public decimal TienPhatTraTre { get; set; }
    public decimal TienPhatHuHong { get; set; }
    public decimal TongTien => TienPhatTraTre + TienPhatHuHong;
}

/// <summary>Số liệu thống kê gọn dùng cho bảng điều khiển (thủ thư / quản trị).</summary>
public class ThongKeBangDieuKhienViewModel
{
    /// <summary>Số tài liệu số trong kho.</summary>
    public int SoTaiLieuSo { get; set; }

    /// <summary>Số sách vật lý trong kho.</summary>
    public int SoSachVatLi { get; set; }

    public int TongLuotXem { get; set; }
    public int LuotXemTaiLieuSo { get; set; }
    public int LuotXemSachVatLi { get; set; }
    public int TongLuotTai { get; set; }
    public int TongLuotMuon { get; set; }
    public int TongLuotTra { get; set; }
    public decimal TongTienPhat { get; set; }

    /// <summary>Danh sách tài liệu được mượn nhiều nhất.</summary>
    public List<ThongKeSachMuonNhieu> SachMuonNhieuNhat { get; set; } = new();

    /// <summary>Danh sách tài liệu được xem/đọc nhiều nhất.</summary>
    public List<ThongKeTaiLieuXemNhieu> TaiLieuXemNhieuNhat { get; set; } = new();
}

/// <summary>Tổng hợp toàn bộ báo cáo thống kê theo khoảng thời gian.</summary>
public class BaoCaoThongKeViewModel
{
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }

    /// <summary>Loại báo cáo: rỗng (tổng hợp), "taidoc", "muontra", "phat".</summary>
    public string? LoaiBaoCao { get; set; }

    public ThongKeTaiLieuViewModel TaiLieu { get; set; } = new();
    public ThongKeMuonTraViewModel MuonTra { get; set; } = new();
    public ThongKeHoatDongTaiKhoanViewModel TaiKhoan { get; set; } = new();

    /// <summary>Danh sách lượt tải/đọc tài liệu trong kỳ.</summary>
    public List<ChiTietTaiDoc> DanhSachTaiDoc { get; set; } = new();

    /// <summary>Thống kê từng tài liệu theo lượt xem, mượn và trả.</summary>
    public List<ThongKeTaiLieuTheoLuot> DanhSachThongKeTaiLieu { get; set; } = new();

    /// <summary>Thống kê lượt phạt và số tiền phạt theo từng tài khoản.</summary>
    public List<ThongKePhatTheoTaiKhoan> DanhSachPhatTheoTaiKhoan { get; set; } = new();

    /// <summary>Cho biết khoảng thời gian được chọn không phát sinh giao dịch mượn/trả.</summary>
    public bool KhongPhatSinhGiaoDich => MuonTra.TongLuotMuon == 0;
}
