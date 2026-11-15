using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Services;

public interface INhatKyService
{
    /// <summary>Ghi một dòng nhật ký hoạt động; không ném lỗi ra ngoài luồng nghiệp vụ chính.</summary>
    Task GhiAsync(string? maNguoiDung, string hanhDong, string? doiTuong = null, string? moTa = null, string? diaChiIp = null);

    Task<NhatKyDanhSachViewModel> LayDanhSachAsync(string? hanhDong, string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, int soDong = 300);

    Task<int> DemLuotDangNhapAsync(DateTime? tuNgay = null);

    Task<List<NhatKyItem>> LayDangNhapGanNhatAsync(int soLuong);
}

/// <summary>Nghiệp vụ nhật ký hoạt động hệ thống (đăng nhập, xem/tải tài liệu, mượn trả, quản trị).</summary>
public class NhatKyService : INhatKyService
{
    private readonly ThuVienDbContext _db;
    private readonly IServiceScopeFactory _phamViFactory;
    private readonly ILogger<NhatKyService> _nhatKy;

    public NhatKyService(
        ThuVienDbContext db,
        IServiceScopeFactory phamViFactory,
        ILogger<NhatKyService> nhatKy)
    {
        _db = db;
        _phamViFactory = phamViFactory;
        _nhatKy = nhatKy;
    }

    public async Task GhiAsync(string? maNguoiDung, string hanhDong, string? doiTuong = null, string? moTa = null, string? diaChiIp = null)
    {
        try
        {
            using var phamVi = _phamViFactory.CreateScope();
            var db = phamVi.ServiceProvider.GetRequiredService<ThuVienDbContext>();

            db.NhatKyHoatDong.Add(new NhatKyHoatDong
            {
                MaNguoiDung = string.IsNullOrWhiteSpace(maNguoiDung) ? null : maNguoiDung,
                HanhDong = hanhDong,
                DoiTuong = doiTuong,
                MoTa = moTa,
                DiaChiIp = diaChiIp,
                ThoiDiem = DateTime.Now
            });

            await db.SaveChangesAsync();
        }
        catch (Exception loi)
        {
            _nhatKy.LogWarning(loi, "Không ghi được nhật ký hoạt động {HanhDong}.", hanhDong);
        }
    }

    public async Task<NhatKyDanhSachViewModel> LayDanhSachAsync(string? hanhDong, string? tuKhoa, DateTime? tuNgay, DateTime? denNgay, int soDong = 300)
    {
        var truyVan = _db.NhatKyHoatDong.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(hanhDong))
        {
            truyVan = truyVan.Where(k => k.HanhDong == hanhDong);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = tuKhoa.Trim();
            truyVan = truyVan.Where(k =>
                (k.MaNguoiDung != null && k.MaNguoiDung.Contains(tk)) ||
                (k.NguoiDung != null && k.NguoiDung.HoTen.Contains(tk)) ||
                (k.DoiTuong != null && k.DoiTuong.Contains(tk)) ||
                (k.MoTa != null && k.MoTa.Contains(tk)));
        }

        if (tuNgay.HasValue)
        {
            var tu = tuNgay.Value.Date;
            truyVan = truyVan.Where(k => k.ThoiDiem >= tu);
        }

        if (denNgay.HasValue)
        {
            var den = denNgay.Value.Date.AddDays(1).AddTicks(-1);
            truyVan = truyVan.Where(k => k.ThoiDiem <= den);
        }

        var danhSach = await truyVan
            .OrderByDescending(k => k.ThoiDiem)
            .Take(soDong)
            .Select(k => new NhatKyItem
            {
                MaNhatKy = k.MaNhatKy,
                MaNguoiDung = k.MaNguoiDung,
                HoTen = k.NguoiDung != null ? k.NguoiDung.HoTen : null,
                HanhDong = k.HanhDong,
                DoiTuong = k.DoiTuong,
                MoTa = k.MoTa,
                DiaChiIp = k.DiaChiIp,
                ThoiDiem = k.ThoiDiem
            })
            .ToListAsync();

        var danhSachHanhDong = await _db.NhatKyHoatDong
            .AsNoTracking()
            .Select(k => k.HanhDong)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return new NhatKyDanhSachViewModel
        {
            HanhDong = hanhDong,
            TuKhoa = tuKhoa,
            TuNgay = tuNgay,
            DenNgay = denNgay,
            DanhSach = danhSach,
            DanhSachHanhDong = danhSachHanhDong
        };
    }

    public async Task<int> DemLuotDangNhapAsync(DateTime? tuNgay = null)
    {
        var truyVan = _db.NhatKyHoatDong
            .AsNoTracking()
            .Where(k => k.HanhDong == HanhDongNhatKy.DangNhap);

        if (tuNgay.HasValue)
        {
            var tu = tuNgay.Value.Date;
            truyVan = truyVan.Where(k => k.ThoiDiem >= tu);
        }

        return await truyVan.CountAsync();
    }

    public async Task<List<NhatKyItem>> LayDangNhapGanNhatAsync(int soLuong)
    {
        return await _db.NhatKyHoatDong
            .AsNoTracking()
            .Where(k => k.HanhDong == HanhDongNhatKy.DangNhap || k.HanhDong == HanhDongNhatKy.DangXuat)
            .OrderByDescending(k => k.ThoiDiem)
            .Take(soLuong)
            .Select(k => new NhatKyItem
            {
                MaNhatKy = k.MaNhatKy,
                MaNguoiDung = k.MaNguoiDung,
                HoTen = k.NguoiDung != null ? k.NguoiDung.HoTen : null,
                HanhDong = k.HanhDong,
                DiaChiIp = k.DiaChiIp,
                ThoiDiem = k.ThoiDiem
            })
            .ToListAsync();
    }
}
