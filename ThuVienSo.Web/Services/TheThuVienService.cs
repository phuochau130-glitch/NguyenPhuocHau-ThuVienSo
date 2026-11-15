using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Services;

public interface ITheThuVienService
{
    Task<TheThuVien?> LayTheCuaDocGiaAsync(string maDocGia);
    Task<KetQua> DangKyTheAsync(string maNguoiDung, DangKyTheViewModel model);
    Task<KetQua> CapNhatThongTinDocGiaAsync(string maNguoiDung, ChinhSuaTheViewModel model);

    /// <summary>Xác nhận độc giả đã thanh toán lệ phí thẻ sau bước thanh toán.</summary>
    Task<KetQua> ThanhToanTheAsync(string maDocGia);

    Task<List<TheThuVien>> LayDanhSachAsync(string? trangThai, string? tuKhoa, bool? daThanhToan = null);
    Task<TheThuVien?> LayTheoMaAsync(string maThe);
    Task<KetQua> DuyetTheAsync(string maThe);
    Task<KetQua> TuChoiTheAsync(string maThe, string lyDo);
    Task<KetQua> GiaHanTheAsync(string maThe, DateOnly ngayHetHanMoi);
    Task<KetQua> CapNhatTheAsync(string maThe, DateOnly ngayCap, DateOnly ngayHetHan, string? ghiChu);
    Task<KetQua> DoiTrangThaiAsync(string maThe, string trangThai, string? ghiChu);
    Task<KetQua> DoiThanhToanAsync(string maThe, bool daThanhToan);
    Task<KetQua> XoaTheAsync(string maThe);
}

/// <summary>Nghiệp vụ quản lý thẻ thư viện (PH-03 đăng ký, PH-05 quản lý thẻ).</summary>
public class TheThuVienService : ITheThuVienService
{
    private static readonly string[] TrangThaiHopLe =
    {
        TrangThaiThe.ChoDuyet, TrangThaiThe.HoatDong, TrangThaiThe.HetHan,
        TrangThaiThe.BiKhoa, TrangThaiThe.TuChoi
    };

    private readonly ThuVienDbContext _db;

    public TheThuVienService(ThuVienDbContext db) => _db = db;

    public async Task<TheThuVien?> LayTheCuaDocGiaAsync(string maDocGia)
    {
        return await _db.TheThuVien
            .AsNoTracking()
            .Include(t => t.DocGia).ThenInclude(d => d.NguoiDung)
            .FirstOrDefaultAsync(t => t.MaDocGia == maDocGia);
    }

    public async Task<TheThuVien?> LayTheoMaAsync(string maThe)
    {
        return await _db.TheThuVien
            .Include(t => t.DocGia).ThenInclude(d => d.NguoiDung)
            .FirstOrDefaultAsync(t => t.MaThe == maThe);
    }

    public async Task<KetQua> DangKyTheAsync(string maNguoiDung, DangKyTheViewModel model)
    {
        var docGia = await _db.DocGia
            .Include(d => d.NguoiDung)
            .Include(d => d.TheThuVien)
            .FirstOrDefaultAsync(d => d.MaDocGia == maNguoiDung);

        if (docGia is null)
        {
            return KetQua.Loi("Chỉ tài khoản độc giả (sinh viên, giảng viên) mới được đăng ký thẻ thư viện.");
        }

        var theCu = docGia.TheThuVien;
        if (theCu is not null &&
            !theCu.DaHetHan &&
            theCu.TrangThai != TrangThaiThe.TuChoi)
        {
            return theCu.TrangThai switch
            {
                TrangThaiThe.ChoDuyet => KetQua.Loi("Hồ sơ đăng ký thẻ của bạn đang chờ thủ thư xét duyệt."),
                TrangThaiThe.HoatDong => KetQua.Loi("Bạn đã có thẻ thư viện đang hoạt động."),
                TrangThaiThe.BiKhoa => KetQua.Loi("Thẻ thư viện của bạn đang bị khóa, vui lòng liên hệ thủ thư."),
                _ => KetQua.Loi("Bạn đã có thẻ thư viện, vui lòng kiểm tra trạng thái thẻ.")
            };
        }

        var trungMa = await _db.DocGia.AnyAsync(d =>
            d.MaSinhVienHoacCanBo == model.MaSinhVienHoacCanBo && d.MaDocGia != maNguoiDung);
        if (trungMa)
        {
            return KetQua.Loi("Mã sinh viên / cán bộ đã được sử dụng cho một tài khoản khác.");
        }

        var trungEmail = await _db.NguoiDung.AnyAsync(n =>
            n.Email == model.Email && n.MaNguoiDung != maNguoiDung);
        if (trungEmail)
        {
            return KetQua.Loi("Email liên hệ đã được sử dụng cho một tài khoản khác.");
        }

        var nguoiDung = docGia.NguoiDung;
        nguoiDung.HoTen = model.HoTen.Trim();
        nguoiDung.Email = model.Email.Trim();
        nguoiDung.SoDienThoai = model.SoDienThoai.Trim();
        docGia.MaSinhVienHoacCanBo = model.MaSinhVienHoacCanBo.Trim();
        docGia.Khoa = model.Khoa?.Trim();

        var homNay = DateOnly.FromDateTime(DateTime.Today);

        if (theCu is not null)
        {
            theCu.TrangThai = TrangThaiThe.ChoDuyet;
            theCu.GhiChu = null;
            theCu.NgayGiaHan = homNay;
            theCu.NgayHetHan = homNay.AddYears(1);
            theCu.LePhi = QuyDinh.LePhiTheThuVien;
            theCu.DaThanhToan = false;
            theCu.NgayThanhToan = null;
            await _db.SaveChangesAsync();
            return KetQua.Ok($"Đã gửi hồ sơ đăng ký lại thẻ {theCu.MaThe}. Vui lòng thanh toán lệ phí để hoàn tất hồ sơ.");
        }

        var maThe = MaTuDong.TiepTheo("TTV", await _db.TheThuVien.Select(t => t.MaThe).ToListAsync());

        _db.TheThuVien.Add(new TheThuVien
        {
            MaThe = maThe,
            MaDocGia = docGia.MaDocGia,
            NgayCap = homNay,
            NgayGiaHan = homNay,
            NgayHetHan = homNay.AddYears(1),
            TrangThai = TrangThaiThe.ChoDuyet,
            LePhi = QuyDinh.LePhiTheThuVien,
            DaThanhToan = false,
            NgayThanhToan = null,
            GhiChu = null
        });

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã tạo hồ sơ đăng ký thẻ thư viện (mã dự kiến {maThe}). Vui lòng thanh toán lệ phí để hoàn tất hồ sơ.");
    }

    /// <summary>Ghi nhận độc giả đã thanh toán lệ phí thẻ; hồ sơ chuyển sang chờ thủ thư duyệt và kiểm tra.</summary>
    public async Task<KetQua> ThanhToanTheAsync(string maDocGia)
    {
        var the = await _db.TheThuVien.FirstOrDefaultAsync(t => t.MaDocGia == maDocGia);
        if (the is null)
        {
            return KetQua.Loi("Không tìm thấy hồ sơ thẻ thư viện để thanh toán.");
        }

        if (the.DaThanhToan)
        {
            return KetQua.Ok($"Thẻ {the.MaThe} đã được thanh toán lệ phí trước đó.");
        }

        the.DaThanhToan = true;
        the.NgayThanhToan = DateTime.Now;
        if (the.LePhi <= 0)
        {
            the.LePhi = QuyDinh.LePhiTheThuVien;
        }

        if (the.TrangThai == TrangThaiThe.TuChoi)
        {
            the.TrangThai = TrangThaiThe.ChoDuyet;
        }

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã thanh toán lệ phí {the.LePhi:N0} VNĐ cho thẻ {the.MaThe}. Hồ sơ đang chờ thủ thư duyệt và kiểm tra.");
    }

    /// <summary>Cập nhật thông tin cá nhân gắn với thẻ thư viện của độc giả đang đăng nhập.</summary>
    public async Task<KetQua> CapNhatThongTinDocGiaAsync(string maNguoiDung, ChinhSuaTheViewModel model)
    {
        var docGia = await _db.DocGia
            .Include(d => d.NguoiDung)
            .Include(d => d.TheThuVien)
            .FirstOrDefaultAsync(d => d.MaDocGia == maNguoiDung);

        if (docGia is null)
        {
            return KetQua.Loi("Chỉ tài khoản độc giả mới có thẻ thư viện để chỉnh sửa.");
        }

        if (docGia.TheThuVien is null)
        {
            return KetQua.Loi("Bạn chưa có thẻ thư viện để chỉnh sửa thông tin.");
        }

        var email = model.Email.Trim();
        var maSo = model.MaSinhVienHoacCanBo.Trim();

        var trungMa = await _db.DocGia.AnyAsync(d =>
            d.MaSinhVienHoacCanBo == maSo && d.MaDocGia != maNguoiDung);
        if (trungMa)
        {
            return KetQua.Loi("Mã sinh viên / cán bộ đã được sử dụng cho một tài khoản khác.");
        }

        var trungEmail = await _db.NguoiDung.AnyAsync(n =>
            n.Email == email && n.MaNguoiDung != maNguoiDung);
        if (trungEmail)
        {
            return KetQua.Loi("Email liên hệ đã được sử dụng cho một tài khoản khác.");
        }

        docGia.NguoiDung.HoTen = model.HoTen.Trim();
        docGia.NguoiDung.Email = email;
        docGia.NguoiDung.SoDienThoai = model.SoDienThoai.Trim();
        docGia.MaSinhVienHoacCanBo = maSo;
        docGia.Khoa = string.IsNullOrWhiteSpace(model.Khoa) ? null : model.Khoa.Trim();

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã cập nhật thông tin thẻ {docGia.TheThuVien.MaThe}.");
    }

    public async Task<List<TheThuVien>> LayDanhSachAsync(string? trangThai, string? tuKhoa, bool? daThanhToan = null)
    {
        var truyVan = _db.TheThuVien
            .AsNoTracking()
            .Include(t => t.DocGia).ThenInclude(d => d.NguoiDung)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            truyVan = truyVan.Where(t => t.TrangThai == trangThai);
        }

        if (daThanhToan.HasValue)
        {
            truyVan = truyVan.Where(t => t.DaThanhToan == daThanhToan.Value);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = tuKhoa.Trim();
            truyVan = truyVan.Where(t =>
                t.MaThe.Contains(tk) ||
                t.DocGia.NguoiDung.HoTen.Contains(tk) ||
                t.DocGia.MaSinhVienHoacCanBo.Contains(tk));
        }

        return await truyVan
            .OrderBy(t => t.TrangThai == TrangThaiThe.ChoDuyet ? 0 : 1)
            .ThenBy(t => t.MaThe)
            .ToListAsync();
    }

    public async Task<KetQua> DuyetTheAsync(string maThe)
    {
        var the = await _db.TheThuVien.FirstOrDefaultAsync(t => t.MaThe == maThe);
        if (the is null) return KetQua.Loi("Không tìm thấy thẻ thư viện.");

        if (the.TrangThai != TrangThaiThe.ChoDuyet && the.TrangThai != TrangThaiThe.TuChoi)
        {
            return KetQua.Loi($"Chỉ có thể phê duyệt thẻ đang chờ duyệt (trạng thái hiện tại: {the.TrangThai}).");
        }

        if (!the.DaThanhToan)
        {
            return KetQua.Loi("Hồ sơ chưa thanh toán lệ phí. Vui lòng kiểm tra thanh toán trước khi phê duyệt.");
        }

        var homNay = DateOnly.FromDateTime(DateTime.Today);
        the.TrangThai = TrangThaiThe.HoatDong;
        the.NgayCap = homNay;
        the.NgayGiaHan = homNay;
        if (the.NgayHetHan < homNay)
        {
            the.NgayHetHan = homNay.AddYears(1);
        }
        the.GhiChu = null;

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã phê duyệt và kích hoạt thẻ {the.MaThe}.");
    }

    public async Task<KetQua> TuChoiTheAsync(string maThe, string lyDo)
    {
        if (string.IsNullOrWhiteSpace(lyDo))
        {
            return KetQua.Loi("Vui lòng nhập lý do từ chối.");
        }

        var the = await _db.TheThuVien.FirstOrDefaultAsync(t => t.MaThe == maThe);
        if (the is null) return KetQua.Loi("Không tìm thấy thẻ thư viện.");

        the.TrangThai = TrangThaiThe.TuChoi;
        the.GhiChu = lyDo.Trim();
        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã từ chối hồ sơ thẻ {the.MaThe}.");
    }

    public async Task<KetQua> GiaHanTheAsync(string maThe, DateOnly ngayHetHanMoi)
    {
        var the = await _db.TheThuVien.FirstOrDefaultAsync(t => t.MaThe == maThe);
        if (the is null) return KetQua.Loi("Không tìm thấy thẻ thư viện.");

        if (ngayHetHanMoi <= the.NgayHetHan)
        {
            return KetQua.Loi("Ngày hết hạn mới phải sau ngày hết hạn hiện tại.");
        }

        the.NgayHetHan = ngayHetHanMoi;
        the.NgayGiaHan = DateOnly.FromDateTime(DateTime.Today);
        if (the.TrangThai == TrangThaiThe.HetHan)
        {
            the.TrangThai = TrangThaiThe.HoatDong;
        }
        the.GhiChu = null;

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã gia hạn thẻ {the.MaThe} đến ngày {ngayHetHanMoi:dd/MM/yyyy}.");
    }

    public async Task<KetQua> CapNhatTheAsync(string maThe, DateOnly ngayCap, DateOnly ngayHetHan, string? ghiChu)
    {
        var the = await _db.TheThuVien.FirstOrDefaultAsync(t => t.MaThe == maThe);
        if (the is null) return KetQua.Loi("Không tìm thấy thẻ thư viện.");

        if (ngayHetHan <= ngayCap)
        {
            return KetQua.Loi("Ngày hết hạn phải sau ngày cấp.");
        }

        the.NgayCap = ngayCap;
        the.NgayGiaHan = ngayCap;
        the.NgayHetHan = ngayHetHan;
        the.GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã cập nhật thông tin thẻ {the.MaThe}.");
    }

    /// <summary>Thủ thư xác nhận hoặc bỏ xác nhận trạng thái thanh toán lệ phí của thẻ.</summary>
    public async Task<KetQua> DoiThanhToanAsync(string maThe, bool daThanhToan)
    {
        var the = await _db.TheThuVien.FirstOrDefaultAsync(t => t.MaThe == maThe);
        if (the is null) return KetQua.Loi("Không tìm thấy thẻ thư viện.");

        the.DaThanhToan = daThanhToan;
        the.NgayThanhToan = daThanhToan ? DateTime.Now : null;
        if (the.LePhi <= 0)
        {
            the.LePhi = QuyDinh.LePhiTheThuVien;
        }

        await _db.SaveChangesAsync();
        return KetQua.Ok(daThanhToan
            ? $"Đã ghi nhận thanh toán lệ phí thẻ {the.MaThe}."
            : $"Đã chuyển thẻ {the.MaThe} về trạng thái chưa thanh toán.");
    }

    public async Task<KetQua> DoiTrangThaiAsync(string maThe, string trangThai, string? ghiChu)
    {
        if (!TrangThaiHopLe.Contains(trangThai))
        {
            return KetQua.Loi("Trạng thái thẻ không hợp lệ.");
        }

        var the = await _db.TheThuVien.FirstOrDefaultAsync(t => t.MaThe == maThe);
        if (the is null) return KetQua.Loi("Không tìm thấy thẻ thư viện.");

        the.TrangThai = trangThai;
        the.GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? the.GhiChu : ghiChu.Trim();

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã chuyển thẻ {the.MaThe} sang trạng thái \"{trangThai}\".");
    }

    public async Task<KetQua> XoaTheAsync(string maThe)
    {
        var the = await _db.TheThuVien
            .Include(t => t.DocGia)
                .ThenInclude(d => d.DanhSachPhieuMuon)
            .FirstOrDefaultAsync(t => t.MaThe == maThe);

        if (the is null) return KetQua.Loi("Không tìm thấy thẻ thư viện.");

        var dangMuon = the.DocGia.DanhSachPhieuMuon
            .Any(p => p.TrangThai == TrangThaiPhieuMuon.DangMuon || p.TrangThai == TrangThaiPhieuMuon.DangGiu);

        if (dangMuon)
        {
            return KetQua.Loi("Không thể xóa thẻ: độc giả đang có phiếu mượn chưa hoàn tất. Hãy khóa thẻ thay vì xóa.");
        }

        var chuaNopPhat = the.DocGia.DanhSachPhieuMuon.Any(p => p.TienPhat > 0 && p.TrangThai != TrangThaiPhieuMuon.DaTra);
        if (chuaNopPhat)
        {
            return KetQua.Loi("Không thể xóa thẻ: độc giả còn khoản tiền phạt chưa thanh toán.");
        }

        _db.TheThuVien.Remove(the);
        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã xóa thẻ {maThe}.");
    }
}
