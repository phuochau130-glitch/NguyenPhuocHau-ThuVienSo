using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Services;

public interface IQuanLySachService
{
    Task<List<TaiLieu>> LayDanhSachAsync(string? tuKhoa);

    /// <summary>Lấy chi tiết một sách vật lý để thủ thư xem, không phụ thuộc trạng thái duyệt.</summary>
    Task<TaiLieu?> LayChiTietAsync(string maSach);
    Task<SachVatLiFormViewModel?> LayFormSuaAsync(string maSach);
    Task<KetQua> ThemAsync(SachVatLiFormViewModel model);
    Task<KetQua> SuaAsync(SachVatLiFormViewModel model);
    Task<KetQua> XoaAsync(string maSach);
}

/// <summary>Nghiệp vụ quản lý kho sách vật lý của thủ thư: thêm, sửa, xóa.</summary>
public class QuanLySachService : IQuanLySachService
{
    private readonly ThuVienDbContext _db;
    private readonly ITepLuuTru _tepLuuTru;
    private readonly IWebHostEnvironment _moiTruong;
    private readonly ILogger<QuanLySachService> _logger;

    public QuanLySachService(
        ThuVienDbContext db,
        ITepLuuTru tepLuuTru,
        IWebHostEnvironment moiTruong,
        ILogger<QuanLySachService> logger)
    {
        _db = db;
        _tepLuuTru = tepLuuTru;
        _moiTruong = moiTruong;
        _logger = logger;
    }

    public async Task<List<TaiLieu>> LayDanhSachAsync(string? tuKhoa)
    {
        var truyVan = _db.TaiLieu
            .AsNoTracking()
            .Include(t => t.BanSach).ThenInclude(s => s!.DanhSachPhieuMuon)
            .Where(t => t.BanSach != null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = tuKhoa.Trim();
            var tuKhoaChuanHoa = tk.ToUpperInvariant();
            truyVan = truyVan.Where(t =>
                t.MaTaiLieu.ToUpper().Contains(tuKhoaChuanHoa) ||
                t.TenTaiLieu.ToUpper().Contains(tuKhoaChuanHoa) ||
                (t.TacGia != null && t.TacGia.ToUpper().Contains(tuKhoaChuanHoa)));
        }

        return await truyVan.OrderBy(t => t.MaTaiLieu).ToListAsync();
    }

    public async Task<TaiLieu?> LayChiTietAsync(string maSach)
    {
        return await _db.TaiLieu
            .AsNoTracking()
            .Include(t => t.BanSach).ThenInclude(s => s!.DanhSachPhieuMuon).ThenInclude(p => p.DocGia).ThenInclude(d => d.NguoiDung)
            .FirstOrDefaultAsync(t => t.MaTaiLieu == maSach && t.BanSach != null);
    }

    public async Task<SachVatLiFormViewModel?> LayFormSuaAsync(string maSach)
    {
        var sach = await _db.SachVatLi
            .AsNoTracking()
            .Include(s => s.TaiLieu)
            .FirstOrDefaultAsync(s => s.MaSach == maSach);
        if (sach is null) return null;

        var tl = sach.TaiLieu;
        return new SachVatLiFormViewModel
        {
            MaSach = sach.MaSach,
            SoLuongKhaDungHienTai = sach.SoLuongKhaDung,
            TenTaiLieu = tl.TenTaiLieu,
            TacGia = tl.TacGia,
            NhaXuatBan = tl.NhaXuatBan,
            NamXuatBan = tl.NamXuatBan,
            DanhMuc = tl.DanhMuc,
            TongSoLuong = sach.TongSoLuong,
            ViTriKeSach = sach.ViTriKeSach,
            CoTepXemTruoc = !string.IsNullOrWhiteSpace(sach.DuongDanTepXemTruoc),
            DinhDangTepXemTruocHienTai = sach.DinhDangTepXemTruoc,
            CoAnhBia = !string.IsNullOrWhiteSpace(tl.DuongDanAnhBia),
            DuongDanAnhBiaHienTai = tl.DuongDanAnhBia,
            GhiChu = tl.GhiChu
        };
    }

    public async Task<KetQua> ThemAsync(SachVatLiFormViewModel model)
    {
        var maSach = MaTuDong.TiepTheo("TL", await _db.TaiLieu.Select(t => t.MaTaiLieu).ToListAsync());

        var taiLieu = new TaiLieu
        {
            MaTaiLieu = maSach,
            TrangThaiDuyet = TrangThaiDuyetTaiLieu.DaDuyet
        };
        GanThongTinTaiLieu(taiLieu, model);

        var sach = new SachVatLi
        {
            MaSach = maSach,
            MaVach = $"TV-{maSach}",
            TongSoLuong = model.TongSoLuong,
            SoLuongKhaDung = model.TongSoLuong,
            ViTriKeSach = string.IsNullOrWhiteSpace(model.ViTriKeSach) ? null : model.ViTriKeSach.Trim()
        };
        if (model.TepXemTruoc is not null && model.TepXemTruoc.Length > 0)
        {
            var (thanhCong, thongBao, duongDan) = await _tepLuuTru.LuuTepTaiLieuAsync(model.TepXemTruoc, "xemtruoc");
            if (!thanhCong || duongDan is null)
            {
                return KetQua.Loi($"Không lưu được tệp đọc thử: {thongBao}");
            }

            sach.DuongDanTepXemTruoc = duongDan;
            sach.DinhDangTepXemTruoc = Path.GetExtension(model.TepXemTruoc.FileName).TrimStart('.').ToUpperInvariant();
        }

        if (model.AnhBia is not null && model.AnhBia.Length > 0)
        {
            var (thanhCong, thongBao, duongDan) = await _tepLuuTru.LuuAnhBiaAsync(model.AnhBia, "anhbia");
            if (!thanhCong || duongDan is null)
            {
                return KetQua.Loi($"Không lưu được ảnh bìa: {thongBao}");
            }

            taiLieu.DuongDanAnhBia = duongDan;
        }

        _db.TaiLieu.Add(taiLieu);
        _db.SachVatLi.Add(sach);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException loi)
        {
            _logger.LogError(loi, "Không thể lưu sách mới {MaSach}; lỗi cơ sở dữ liệu: {ChiTiet}",
                maSach, loi.GetBaseException().Message);
            XoaTepTrenDisk(sach.DuongDanTepXemTruoc);
            XoaTepTrenDisk(taiLieu.DuongDanAnhBia);
            return KetQua.Loi("Không thể thêm sách do cơ sở dữ liệu từ chối bản ghi. Hãy kiểm tra nhật ký ứng dụng để xem chi tiết.");
        }

        return KetQua.Ok($"Đã thêm sách \"{taiLieu.TenTaiLieu}\" vào kho (mã {maSach}).");
    }

    public async Task<KetQua> SuaAsync(SachVatLiFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.MaSach))
        {
            return KetQua.Loi("Thiếu mã sách cần cập nhật.");
        }

        var sach = await _db.SachVatLi
            .Include(s => s.TaiLieu)
            .FirstOrDefaultAsync(s => s.MaSach == model.MaSach);
        if (sach is null) return KetQua.Loi("Không tìm thấy sách vật lý.");

        var soDangMuon = await _db.PhieuMuon.CountAsync(p => p.MaSach == model.MaSach &&
            (p.TrangThai == TrangThaiPhieuMuon.DangMuon || p.TrangThai == TrangThaiPhieuMuon.DangGiu));
        if (model.TongSoLuong < soDangMuon)
        {
            return KetQua.Loi($"Không thể giảm tổng số lượng xuống dưới số bản đang cho mượn ({soDangMuon} bản).");
        }

        GanThongTinTaiLieu(sach.TaiLieu, model);

        var chenhLech = model.TongSoLuong - sach.TongSoLuong;
        sach.TongSoLuong = model.TongSoLuong;
        sach.SoLuongKhaDung = Math.Clamp(sach.SoLuongKhaDung + chenhLech, 0, model.TongSoLuong);
        sach.ViTriKeSach = string.IsNullOrWhiteSpace(model.ViTriKeSach) ? null : model.ViTriKeSach.Trim();
        if (model.TepXemTruoc is not null && model.TepXemTruoc.Length > 0)
        {
            var (thanhCong, thongBao, duongDan) = await _tepLuuTru.LuuTepTaiLieuAsync(model.TepXemTruoc, "xemtruoc");
            if (!thanhCong || duongDan is null)
            {
                return KetQua.Loi($"Không lưu được tệp đọc thử: {thongBao}");
            }

            sach.DuongDanTepXemTruoc = duongDan;
            sach.DinhDangTepXemTruoc = Path.GetExtension(model.TepXemTruoc.FileName).TrimStart('.').ToUpperInvariant();
        }

        if (model.AnhBia is not null && model.AnhBia.Length > 0)
        {
            var (thanhCong, thongBao, duongDan) = await _tepLuuTru.LuuAnhBiaAsync(model.AnhBia, "anhbia");
            if (!thanhCong || duongDan is null)
            {
                return KetQua.Loi($"Không lưu được ảnh bìa: {thongBao}");
            }

            XoaTepTrenDisk(sach.TaiLieu.DuongDanAnhBia);
            sach.TaiLieu.DuongDanAnhBia = duongDan;
        }

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã cập nhật sách \"{sach.TaiLieu.TenTaiLieu}\" (mã {sach.MaSach}).");
    }

    public async Task<KetQua> XoaAsync(string maSach)
    {
        var sach = await _db.SachVatLi
            .Include(s => s.TaiLieu)
            .Include(s => s.DanhSachPhieuMuon)
            .FirstOrDefaultAsync(s => s.MaSach == maSach);
        if (sach is null) return KetQua.Loi("Không tìm thấy sách vật lý.");

        if (sach.DanhSachPhieuMuon.Count > 0)
        {
            return KetQua.Loi("Không thể xóa sách đã có lịch sử mượn trả. Nếu sách hỏng hoặc mất, hãy sửa tổng số lượng về 0.");
        }

        XoaTepTrenDisk(sach.DuongDanTepXemTruoc);
        XoaTepTrenDisk(sach.TaiLieu.DuongDanAnhBia);

        _db.SachVatLi.Remove(sach);
        _db.TaiLieu.Remove(sach.TaiLieu);
        await _db.SaveChangesAsync();

        return KetQua.Ok($"Đã xóa sách \"{sach.TaiLieu.TenTaiLieu}\" (mã {maSach}) khỏi kho.");
    }

    private static void GanThongTinTaiLieu(TaiLieu taiLieu, SachVatLiFormViewModel model)
    {
        taiLieu.TenTaiLieu = model.TenTaiLieu.Trim();
        taiLieu.TacGia = string.IsNullOrWhiteSpace(model.TacGia) ? null : model.TacGia.Trim();
        taiLieu.NhaXuatBan = string.IsNullOrWhiteSpace(model.NhaXuatBan) ? null : model.NhaXuatBan.Trim();
        taiLieu.NamXuatBan = model.NamXuatBan;
        taiLieu.DanhMuc = string.IsNullOrWhiteSpace(model.DanhMuc) ? null : model.DanhMuc.Trim();
        taiLieu.GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim();
    }

    private void XoaTepTrenDisk(string? duongDan)
    {
        if (string.IsNullOrWhiteSpace(duongDan)) return;

        var duongDanVatLy = Path.Combine(_moiTruong.WebRootPath, duongDan.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(duongDanVatLy))
        {
            File.Delete(duongDanVatLy);
        }
    }
}
