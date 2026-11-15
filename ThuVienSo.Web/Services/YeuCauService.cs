using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;

namespace ThuVienSo.Web.Services;

public interface IYeuCauService
{
    Task<KetQua> GuiYeuCauAsync(string maDocGia, string maTaiLieuSo, string? lyDo);
    Task<List<YeuCauTaiLieu>> LayCuaDocGiaAsync(string maDocGia);
    Task<List<YeuCauTaiLieu>> LayDanhSachAsync(string? trangThai, string? tuKhoa);
    Task<YeuCauTaiLieu?> LayTheoMaAsync(string maYeuCau);
    Task<KetQua> PheDuyetAsync(string maYeuCau, string maThuThu);
    Task<KetQua> TuChoiAsync(string maYeuCau, string maThuThu);
    Task<bool> DaCoQuyenDocAsync(string maDocGia, string maTaiLieuSo);
    Task<YeuCauTaiLieu?> LayYeuCauGanNhatAsync(string maDocGia, string maTaiLieuSo);
    Task<int> DemChoDuyetAsync();
}

/// <summary>Nghiệp vụ yêu cầu xin quyền đọc tài liệu số bị giới hạn (UC-05, PH-04).</summary>
public class YeuCauService : IYeuCauService
{
    private readonly ThuVienDbContext _db;

    public YeuCauService(ThuVienDbContext db) => _db = db;

    public async Task<KetQua> GuiYeuCauAsync(string maDocGia, string maTaiLieuSo, string? lyDo)
    {
        var taiLieuSo = await _db.TaiLieuSo
            .Include(t => t.TaiLieu)
            .FirstOrDefaultAsync(t => t.MaTaiLieuSo == maTaiLieuSo);

        if (taiLieuSo is null) return KetQua.Loi("Không tìm thấy tài liệu số.");

        if (!taiLieuSo.TaiLieu.LaTaiLieuHanChe)
        {
            return KetQua.Loi("Tài liệu này được truy cập tự do, bạn không cần gửi yêu cầu xin quyền.");
        }

        var dangCho = await _db.YeuCauTaiLieu.AnyAsync(y =>
            y.MaDocGia == maDocGia && y.MaTaiLieuSo == maTaiLieuSo && y.TrangThai == TrangThaiYeuCau.ChoDuyet);

        if (dangCho)
        {
            return KetQua.Loi("Yêu cầu cho tài liệu này đang trong quá trình chờ phê duyệt.");
        }

        if (await DaCoQuyenDocAsync(maDocGia, maTaiLieuSo))
        {
            return KetQua.Loi("Bạn đã được cấp quyền đọc tài liệu này.");
        }

        var maYeuCau = MaTuDong.TiepTheo("YC", await _db.YeuCauTaiLieu.Select(y => y.MaYeuCau).ToListAsync());

        _db.YeuCauTaiLieu.Add(new YeuCauTaiLieu
        {
            MaYeuCau = maYeuCau,
            MaDocGia = maDocGia,
            MaTaiLieuSo = maTaiLieuSo,
            MaThuThu = null,
            NgayYeuCau = DateTime.Now,
            LyDoTruyCap = string.IsNullOrWhiteSpace(lyDo) ? null : lyDo.Trim(),
            TrangThai = TrangThaiYeuCau.ChoDuyet
        });

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã gửi yêu cầu xin quyền đọc \"{taiLieuSo.TaiLieu.TenTaiLieu}\" (mã {maYeuCau}).");
    }

    public async Task<List<YeuCauTaiLieu>> LayCuaDocGiaAsync(string maDocGia)
    {
        return await _db.YeuCauTaiLieu
            .AsNoTracking()
            .Include(y => y.TaiLieuSo).ThenInclude(t => t.TaiLieu)
            .Include(y => y.ThuThu).ThenInclude(t => t!.NguoiDung)
            .Where(y => y.MaDocGia == maDocGia)
            .OrderByDescending(y => y.NgayYeuCau)
            .ToListAsync();
    }

    public async Task<List<YeuCauTaiLieu>> LayDanhSachAsync(string? trangThai, string? tuKhoa)
    {
        var truyVan = _db.YeuCauTaiLieu
            .AsNoTracking()
            .Include(y => y.DocGia).ThenInclude(d => d.NguoiDung)
            .Include(y => y.TaiLieuSo).ThenInclude(t => t.TaiLieu)
            .Include(y => y.ThuThu).ThenInclude(t => t!.NguoiDung)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            truyVan = truyVan.Where(y => y.TrangThai == trangThai);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = tuKhoa.Trim();
            truyVan = truyVan.Where(y =>
                y.MaYeuCau.Contains(tk) ||
                y.DocGia.NguoiDung.HoTen.Contains(tk) ||
                y.TaiLieuSo.TaiLieu.TenTaiLieu.Contains(tk));
        }

        return await truyVan
            .OrderBy(y => y.TrangThai == TrangThaiYeuCau.ChoDuyet ? 0 : 1)
            .ThenByDescending(y => y.NgayYeuCau)
            .ToListAsync();
    }

    public async Task<YeuCauTaiLieu?> LayTheoMaAsync(string maYeuCau)
    {
        return await _db.YeuCauTaiLieu
            .Include(y => y.DocGia).ThenInclude(d => d.NguoiDung)
            .Include(y => y.TaiLieuSo).ThenInclude(t => t.TaiLieu)
            .Include(y => y.ThuThu).ThenInclude(t => t!.NguoiDung)
            .FirstOrDefaultAsync(y => y.MaYeuCau == maYeuCau);
    }

    public async Task<KetQua> PheDuyetAsync(string maYeuCau, string maThuThu)
    {
        var yeuCau = await _db.YeuCauTaiLieu.FirstOrDefaultAsync(y => y.MaYeuCau == maYeuCau);
        if (yeuCau is null) return KetQua.Loi("Không tìm thấy yêu cầu.");

        if (yeuCau.TrangThai != TrangThaiYeuCau.ChoDuyet)
        {
            return KetQua.Loi($"Chỉ có thể phê duyệt yêu cầu đang chờ duyệt (trạng thái hiện tại: {yeuCau.TrangThai}).");
        }

        yeuCau.TrangThai = TrangThaiYeuCau.DaPheDuyet;
        yeuCau.MaThuThu = maThuThu;

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã phê duyệt yêu cầu {maYeuCau}. Độc giả đã được cấp quyền đọc tài liệu.");
    }

    public async Task<KetQua> TuChoiAsync(string maYeuCau, string maThuThu)
    {
        var yeuCau = await _db.YeuCauTaiLieu.FirstOrDefaultAsync(y => y.MaYeuCau == maYeuCau);
        if (yeuCau is null) return KetQua.Loi("Không tìm thấy yêu cầu.");

        if (yeuCau.TrangThai != TrangThaiYeuCau.ChoDuyet)
        {
            return KetQua.Loi("Chỉ có thể từ chối yêu cầu đang chờ duyệt.");
        }

        yeuCau.TrangThai = TrangThaiYeuCau.TuChoi;
        yeuCau.MaThuThu = maThuThu;

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã từ chối yêu cầu {maYeuCau}.");
    }

    public async Task<bool> DaCoQuyenDocAsync(string maDocGia, string maTaiLieuSo)
    {
        return await _db.YeuCauTaiLieu.AnyAsync(y =>
            y.MaDocGia == maDocGia && y.MaTaiLieuSo == maTaiLieuSo &&
            y.TrangThai == TrangThaiYeuCau.DaPheDuyet);
    }

    public async Task<YeuCauTaiLieu?> LayYeuCauGanNhatAsync(string maDocGia, string maTaiLieuSo)
    {
        return await _db.YeuCauTaiLieu
            .AsNoTracking()
            .Where(y => y.MaDocGia == maDocGia && y.MaTaiLieuSo == maTaiLieuSo)
            .OrderByDescending(y => y.NgayYeuCau)
            .FirstOrDefaultAsync();
    }

    public async Task<int> DemChoDuyetAsync()
    {
        return await _db.YeuCauTaiLieu.CountAsync(y => y.TrangThai == TrangThaiYeuCau.ChoDuyet);
    }
}
