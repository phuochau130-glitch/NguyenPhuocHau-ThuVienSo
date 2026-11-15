using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Services;

public interface ITraCuuService
{
    Task<List<TaiLieu>> TimKiemAsync(string? tuKhoa, string? danhMuc, string? loai, bool chiConKhaDung);
    Task<List<string>> LayDanhMucAsync();
    Task<TaiLieu?> LayChiTietAsync(string maTaiLieu);
    Task<List<TaiLieuDaDocViewModel>> LayTaiLieuDaDocAsync(string maNguoiDung);
    Task TangLuotTaiAsync(string maTaiLieuSo);
    Task TangLuotXemAsync(string maTaiLieu);
    Task<SachVatLi?> LaySachVatLiAsync(string maSach);
    Task<int> DemTaiLieuChoDuyetAsync();
}

/// <summary>Nghiệp vụ tra cứu, xem chi tiết và đọc tài liệu (PH-02).</summary>
public class TraCuuService : ITraCuuService
{
    private readonly ThuVienDbContext _db;

    public TraCuuService(ThuVienDbContext db) => _db = db;

    public async Task<List<TaiLieu>> TimKiemAsync(string? tuKhoa, string? danhMuc, string? loai, bool chiConKhaDung)
    {
        var truyVan = _db.TaiLieu
            .AsNoTracking()
            .Include(t => t.BanSach)
            .Include(t => t.BanSo)
            .Where(t => t.TrangThaiDuyet == TrangThaiDuyetTaiLieu.DaDuyet);

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = tuKhoa.Trim();
            var tuKhoaChuanHoa = tk.ToUpperInvariant();
            truyVan = truyVan.Where(t =>
                t.TenTaiLieu.ToUpper().Contains(tuKhoaChuanHoa) ||
                (t.TacGia != null && t.TacGia.ToUpper().Contains(tuKhoaChuanHoa)) ||
                (t.NhaXuatBan != null && t.NhaXuatBan.ToUpper().Contains(tuKhoaChuanHoa)) ||
                (t.DanhMuc != null && t.DanhMuc.ToUpper().Contains(tuKhoaChuanHoa)) ||
                (t.BanSach != null && t.BanSach.Isbn != null && t.BanSach.Isbn.ToUpper().Contains(tuKhoaChuanHoa)) ||
                (t.BanSach != null && t.BanSach.MaVach != null && t.BanSach.MaVach.ToUpper().Contains(tuKhoaChuanHoa)));
        }

        if (!string.IsNullOrWhiteSpace(danhMuc))
        {
            var danhMucChuanHoa = danhMuc.Trim().ToUpperInvariant();
            truyVan = truyVan.Where(t => t.DanhMuc != null && t.DanhMuc.ToUpper() == danhMucChuanHoa);
        }

        truyVan = loai?.Trim().ToLowerInvariant() switch
        {
            "sach" => truyVan.Where(t => t.BanSach != null),
            "so" => truyVan.Where(t => t.BanSo != null),
            _ => truyVan
        };

        if (chiConKhaDung)
        {
            truyVan = truyVan.Where(t => t.BanSach != null && t.BanSach.SoLuongKhaDung > 0);
        }

        return await truyVan
            .OrderBy(t => t.TenTaiLieu)
            .ToListAsync();
    }

    public async Task<List<string>> LayDanhMucAsync()
    {
        return await _db.DanhMucTaiLieu
            .AsNoTracking()
            .Select(x => x.TenDanhMuc)
            .Union(_db.TaiLieu
                .AsNoTracking()
                .Where(t => t.TrangThaiDuyet == TrangThaiDuyetTaiLieu.DaDuyet && t.DanhMuc != null)
                .Select(t => t.DanhMuc!))
            .OrderBy(x => x)
            .ToListAsync();
    }

    public async Task<TaiLieu?> LayChiTietAsync(string maTaiLieu)
    {
        return await _db.TaiLieu
            .AsNoTracking()
            .Include(t => t.BanSach)
            .Include(t => t.BanSo)
            .Include(t => t.NguoiDang!.NguoiDung)
            .FirstOrDefaultAsync(t => t.MaTaiLieu == maTaiLieu);
    }

    public async Task<List<TaiLieuDaDocViewModel>> LayTaiLieuDaDocAsync(string maNguoiDung)
    {
        var nhatKy = await _db.NhatKyHoatDong
            .AsNoTracking()
            .Where(k => k.MaNguoiDung == maNguoiDung
                && k.HanhDong == HanhDongNhatKy.XemTaiLieu
                && k.DoiTuong != null)
            .OrderByDescending(k => k.ThoiDiem)
            .Select(k => new { MaTaiLieu = k.DoiTuong!, k.ThoiDiem })
            .Take(500)
            .ToListAsync();

        var lanDocGanNhat = nhatKy
            .GroupBy(k => k.MaTaiLieu)
            .Select(g => new { MaTaiLieu = g.Key, ThoiDiem = g.Max(k => k.ThoiDiem) })
            .OrderByDescending(k => k.ThoiDiem)
            .ToList();
        var maTaiLieu = lanDocGanNhat.Select(k => k.MaTaiLieu).ToList();
        if (maTaiLieu.Count == 0) return new List<TaiLieuDaDocViewModel>();

        var taiLieu = await _db.TaiLieu
            .AsNoTracking()
            .Include(t => t.BanSach)
            .Include(t => t.BanSo)
            .Where(t => maTaiLieu.Contains(t.MaTaiLieu)
                && t.TrangThaiDuyet == TrangThaiDuyetTaiLieu.DaDuyet)
            .ToListAsync();
        var theoMa = taiLieu.ToDictionary(t => t.MaTaiLieu);

        return lanDocGanNhat
            .Where(k => theoMa.ContainsKey(k.MaTaiLieu))
            .Select(k => new TaiLieuDaDocViewModel
            {
                TaiLieu = theoMa[k.MaTaiLieu],
                LanDocGanNhat = k.ThoiDiem
            })
            .ToList();
    }

    public async Task TangLuotTaiAsync(string maTaiLieuSo)
    {
        var banSo = await _db.TaiLieuSo.FirstOrDefaultAsync(x => x.MaTaiLieuSo == maTaiLieuSo);
        if (banSo is not null)
        {
            banSo.SoLuotTai += 1;
            await _db.SaveChangesAsync();
        }
    }

    public async Task TangLuotXemAsync(string maTaiLieu)
    {
        var taiLieu = await _db.TaiLieu.FirstOrDefaultAsync(x => x.MaTaiLieu == maTaiLieu);
        if (taiLieu is not null)
        {
            taiLieu.SoLuotXem += 1;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<SachVatLi?> LaySachVatLiAsync(string maSach)
    {
        return await _db.SachVatLi
            .AsNoTracking()
            .Include(s => s.TaiLieu)
            .FirstOrDefaultAsync(s => s.MaSach == maSach);
    }

    public async Task<int> DemTaiLieuChoDuyetAsync()
    {
        return await _db.TaiLieu.CountAsync(t => t.TrangThaiDuyet == TrangThaiDuyetTaiLieu.ChoDuyet);
    }
}
