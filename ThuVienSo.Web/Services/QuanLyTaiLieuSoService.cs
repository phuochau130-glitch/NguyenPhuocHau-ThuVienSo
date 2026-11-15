using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Services;

public interface IQuanLyTaiLieuSoService
{
    Task<List<TaiLieu>> LayDanhSachAsync(string? tuKhoa);

    /// <summary>Lấy chi tiết một tài liệu số để thủ thư xem, không phụ thuộc trạng thái duyệt.</summary>
    Task<TaiLieu?> LayChiTietAsync(string maTaiLieuSo);
    Task<TaiLieuSoFormViewModel?> LayFormSuaAsync(string maTaiLieuSo);
    Task<KetQua> ThemAsync(TaiLieuSoFormViewModel model, string webRootPath);
    Task<KetQua> SuaAsync(TaiLieuSoFormViewModel model, string webRootPath);
    Task<KetQua> XoaAsync(string maTaiLieuSo, string webRootPath);
}

/// <summary>Nghiệp vụ quản lý kho tài liệu số của thủ thư: thêm, sửa, xóa.</summary>
public class QuanLyTaiLieuSoService : IQuanLyTaiLieuSoService
{
    private readonly ThuVienDbContext _db;
    private readonly ITepLuuTru _tepLuuTru;

    public QuanLyTaiLieuSoService(ThuVienDbContext db, ITepLuuTru tepLuuTru)
    {
        _db = db;
        _tepLuuTru = tepLuuTru;
    }

    public async Task<List<TaiLieu>> LayDanhSachAsync(string? tuKhoa)
    {
        var truyVan = _db.TaiLieu
            .AsNoTracking()
            .Include(t => t.BanSo)
            .Where(t => t.BanSo != null)
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

    public async Task<TaiLieu?> LayChiTietAsync(string maTaiLieuSo)
    {
        return await _db.TaiLieu
            .AsNoTracking()
            .Include(t => t.BanSo)
            .FirstOrDefaultAsync(t => t.MaTaiLieu == maTaiLieuSo && t.BanSo != null);
    }

    public async Task<TaiLieuSoFormViewModel?> LayFormSuaAsync(string maTaiLieuSo)
    {
        var taiLieuSo = await _db.TaiLieuSo
            .AsNoTracking()
            .Include(t => t.TaiLieu)
            .FirstOrDefaultAsync(t => t.MaTaiLieuSo == maTaiLieuSo);
        if (taiLieuSo is null) return null;

        var tl = taiLieuSo.TaiLieu;
        return new TaiLieuSoFormViewModel
        {
            MaTaiLieuSo = taiLieuSo.MaTaiLieuSo,
            DinhDangHienTai = taiLieuSo.DinhDang,
            DungLuongFileHienTai = taiLieuSo.DungLuongFile,
            TenTaiLieu = tl.TenTaiLieu,
            TacGia = tl.TacGia,
            NhaXuatBan = tl.NhaXuatBan,
            NamXuatBan = tl.NamXuatBan,
            DanhMuc = tl.DanhMuc,
            LaTaiLieuHanChe = tl.LaTaiLieuHanChe,
            CoAnhBia = !string.IsNullOrWhiteSpace(tl.DuongDanAnhBia),
            DuongDanAnhBiaHienTai = tl.DuongDanAnhBia,
            GhiChu = tl.GhiChu
        };
    }

    public async Task<KetQua> ThemAsync(TaiLieuSoFormViewModel model, string webRootPath)
    {
        if (model.TepTaiLieu is null || model.TepTaiLieu.Length == 0)
        {
            return KetQua.Loi("Vui lòng chọn tệp tài liệu để tải lên.");
        }

        var (thanhCong, thongBao, duongDan) = await _tepLuuTru.LuuTepTaiLieuAsync(model.TepTaiLieu, "tailieu");
        if (!thanhCong || duongDan is null)
        {
            return KetQua.Loi(thongBao);
        }

        var maTaiLieu = MaTuDong.TiepTheo("TL", await _db.TaiLieu.Select(t => t.MaTaiLieu).ToListAsync());

        var taiLieu = new TaiLieu
        {
            MaTaiLieu = maTaiLieu,
            TrangThaiDuyet = TrangThaiDuyetTaiLieu.DaDuyet
        };
        GanThongTinTaiLieu(taiLieu, model);

        if (model.AnhBia is not null && model.AnhBia.Length > 0)
        {
            var (anhThanhCong, anhThongBao, anhDuongDan) = await _tepLuuTru.LuuAnhBiaAsync(model.AnhBia, "anhbia");
            if (!anhThanhCong || anhDuongDan is null)
            {
                return KetQua.Loi($"Không lưu được ảnh bìa: {anhThongBao}");
            }

            taiLieu.DuongDanAnhBia = anhDuongDan;
        }

        _db.TaiLieu.Add(taiLieu);
        _db.TaiLieuSo.Add(new TaiLieuSo
        {
            MaTaiLieuSo = maTaiLieu,
            DuongDanFile = duongDan,
            DungLuongFile = Math.Round(model.TepTaiLieu.Length / 1024d / 1024d, 2),
            DinhDang = Path.GetExtension(model.TepTaiLieu.FileName).TrimStart('.').ToUpperInvariant(),
            SoLuotTai = 0
        });

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã thêm tài liệu \"{taiLieu.TenTaiLieu}\" vào kho tài liệu số (mã {maTaiLieu}).");
    }

    public async Task<KetQua> SuaAsync(TaiLieuSoFormViewModel model, string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(model.MaTaiLieuSo))
        {
            return KetQua.Loi("Thiếu mã tài liệu cần cập nhật.");
        }

        var taiLieuSo = await _db.TaiLieuSo
            .Include(t => t.TaiLieu)
            .FirstOrDefaultAsync(t => t.MaTaiLieuSo == model.MaTaiLieuSo);
        if (taiLieuSo is null) return KetQua.Loi("Không tìm thấy tài liệu số.");

        GanThongTinTaiLieu(taiLieuSo.TaiLieu, model);

        if (model.TepTaiLieu is not null && model.TepTaiLieu.Length > 0)
        {
            var (thanhCong, thongBao, duongDan) = await _tepLuuTru.LuuTepTaiLieuAsync(model.TepTaiLieu, "tailieu");
            if (!thanhCong || duongDan is null)
            {
                return KetQua.Loi(thongBao);
            }

            XoaTepTrenDisk(webRootPath, taiLieuSo.DuongDanFile);
            taiLieuSo.DuongDanFile = duongDan;
            taiLieuSo.DungLuongFile = Math.Round(model.TepTaiLieu.Length / 1024d / 1024d, 2);
            taiLieuSo.DinhDang = Path.GetExtension(model.TepTaiLieu.FileName).TrimStart('.').ToUpperInvariant();
        }

        if (model.AnhBia is not null && model.AnhBia.Length > 0)
        {
            var (anhThanhCong, anhThongBao, anhDuongDan) = await _tepLuuTru.LuuAnhBiaAsync(model.AnhBia, "anhbia");
            if (!anhThanhCong || anhDuongDan is null)
            {
                return KetQua.Loi($"Không lưu được ảnh bìa: {anhThongBao}");
            }

            XoaTepTrenDisk(webRootPath, taiLieuSo.TaiLieu.DuongDanAnhBia);
            taiLieuSo.TaiLieu.DuongDanAnhBia = anhDuongDan;
        }

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã cập nhật tài liệu \"{taiLieuSo.TaiLieu.TenTaiLieu}\" (mã {taiLieuSo.MaTaiLieuSo}).");
    }

    public async Task<KetQua> XoaAsync(string maTaiLieuSo, string webRootPath)
    {
        var taiLieuSo = await _db.TaiLieuSo
            .Include(t => t.TaiLieu)
            .Include(t => t.DanhSachYeuCau)
            .FirstOrDefaultAsync(t => t.MaTaiLieuSo == maTaiLieuSo);
        if (taiLieuSo is null) return KetQua.Loi("Không tìm thấy tài liệu số.");

        if (taiLieuSo.DanhSachYeuCau.Count > 0)
        {
            return KetQua.Loi("Không thể xóa tài liệu đã có độc giả gửi yêu cầu truy cập. Hãy chuyển sang giới hạn truy cập hoặc từ chối các yêu cầu trước.");
        }

        XoaTepTrenDisk(webRootPath, taiLieuSo.DuongDanFile);
        XoaTepTrenDisk(webRootPath, taiLieuSo.TaiLieu.DuongDanAnhBia);
        _db.TaiLieuSo.Remove(taiLieuSo);
        _db.TaiLieu.Remove(taiLieuSo.TaiLieu);
        await _db.SaveChangesAsync();

        return KetQua.Ok($"Đã xóa tài liệu \"{taiLieuSo.TaiLieu.TenTaiLieu}\" (mã {maTaiLieuSo}) khỏi kho.");
    }

    private static void GanThongTinTaiLieu(TaiLieu taiLieu, TaiLieuSoFormViewModel model)
    {
        taiLieu.TenTaiLieu = model.TenTaiLieu.Trim();
        taiLieu.TacGia = string.IsNullOrWhiteSpace(model.TacGia) ? null : model.TacGia.Trim();
        taiLieu.NhaXuatBan = string.IsNullOrWhiteSpace(model.NhaXuatBan) ? null : model.NhaXuatBan.Trim();
        taiLieu.NamXuatBan = model.NamXuatBan;
        taiLieu.DanhMuc = string.IsNullOrWhiteSpace(model.DanhMuc) ? null : model.DanhMuc.Trim();
        taiLieu.LaTaiLieuHanChe = model.LaTaiLieuHanChe;
        taiLieu.GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim();
    }

    private static void XoaTepTrenDisk(string webRootPath, string? duongDan)
    {
        if (string.IsNullOrWhiteSpace(duongDan)) return;

        var duongDanVatLy = Path.Combine(webRootPath, duongDan.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(duongDanVatLy))
        {
            File.Delete(duongDanVatLy);
        }
    }
}
