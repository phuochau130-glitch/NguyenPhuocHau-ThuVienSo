using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Services;

/// <summary>Lưu trữ tệp tài liệu số vào thư mục wwwroot/uploads.</summary>
public interface ITepLuuTru
{
    Task<(bool ThanhCong, string ThongBao, string? DuongDan)> LuuTepTaiLieuAsync(IFormFile tep, string thuMucCon);

    /// <summary>Lưu ảnh bìa (trang bìa) cho sách vật lý hoặc tài liệu số.</summary>
    Task<(bool ThanhCong, string ThongBao, string? DuongDan)> LuuAnhBiaAsync(IFormFile tep, string thuMucCon);
}

public class TepLuuTru : ITepLuuTru
{
    private const long KichThuocToiDa = 25 * 1024 * 1024;
    private const long KichThuocAnhToiDa = 5 * 1024 * 1024;

    private static readonly string[] DinhDangChoPhep = { ".pdf", ".epub", ".doc", ".docx", ".txt" };
    private static readonly string[] DinhDangAnhChoPhep = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private readonly IWebHostEnvironment _moiTruong;

    public TepLuuTru(IWebHostEnvironment moiTruong) => _moiTruong = moiTruong;

    public Task<(bool ThanhCong, string ThongBao, string? DuongDan)> LuuTepTaiLieuAsync(IFormFile tep, string thuMucCon)
        => LuuTepAsync(tep, thuMucCon, KichThuocToiDa, DinhDangChoPhep, "Tệp tài liệu", "25MB");

    public Task<(bool ThanhCong, string ThongBao, string? DuongDan)> LuuAnhBiaAsync(IFormFile tep, string thuMucCon)
        => LuuTepAsync(tep, thuMucCon, KichThuocAnhToiDa, DinhDangAnhChoPhep, "Ảnh bìa", "5MB");

    private async Task<(bool ThanhCong, string ThongBao, string? DuongDan)> LuuTepAsync(
        IFormFile tep, string thuMucCon, long kichThuocToiDa, string[] dinhDangChoPhep, string nhan, string gioiHan)
    {
        if (tep is null || tep.Length == 0)
        {
            return (false, $"{nhan} không hợp lệ.", null);
        }

        if (tep.Length > kichThuocToiDa)
        {
            return (false, $"{nhan} vượt quá dung lượng cho phép (tối đa {gioiHan}).", null);
        }

        var duoi = Path.GetExtension(tep.FileName).ToLowerInvariant();
        if (!dinhDangChoPhep.Contains(duoi))
        {
            return (false, $"Định dạng {nhan.ToLowerInvariant()} không được hỗ trợ. Chỉ chấp nhận: {string.Join(", ", dinhDangChoPhep)}.", null);
        }

        var thuMucVatLy = Path.Combine(_moiTruong.WebRootPath, "uploads", thuMucCon);
        Directory.CreateDirectory(thuMucVatLy);

        var tenTep = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{duoi}";
        var duongDanVatLy = Path.Combine(thuMucVatLy, tenTep);

        await using (var luong = new FileStream(duongDanVatLy, FileMode.Create))
        {
            await tep.CopyToAsync(luong);
        }

        var duongDanTuongDoi = $"/uploads/{thuMucCon}/{tenTep}";
        return (true, "Lưu tệp thành công.", duongDanTuongDoi);
    }
}

public interface INckhService
{
    Task<KetQua> TaiLenAsync(string maDocGia, TaiLenNckhViewModel model, string webRootPath);
    Task<List<TaiLieuDongGopViewModel>> LayCuaDocGiaAsync(string maDocGia);
    Task<List<TaiLieu>> LayDanhSachAsync(string? trangThai, string? tuKhoa);
    Task<TaiLieu?> LayTheoMaAsync(string maTaiLieu);
    Task<KetQua> PheDuyetAsync(string maTaiLieu);
    Task<KetQua> TuChoiAsync(string maTaiLieu, string lyDo);
    Task<int> DemChoDuyetAsync();
}

/// <summary>Nghiệp vụ đóng góp và kiểm duyệt tài liệu nghiên cứu khoa học (Quy tắc 8, PH-04).</summary>
public class NckhService : INckhService
{
    private readonly ThuVienDbContext _db;
    private readonly ITepLuuTru _tepLuuTru;

    public NckhService(ThuVienDbContext db, ITepLuuTru tepLuuTru)
    {
        _db = db;
        _tepLuuTru = tepLuuTru;
    }

    public async Task<KetQua> TaiLenAsync(string maDocGia, TaiLenNckhViewModel model, string webRootPath)
    {
        var docGia = await _db.DocGia.FirstOrDefaultAsync(d => d.MaDocGia == maDocGia);
        if (docGia is null)
        {
            return KetQua.Loi("Chỉ sinh viên và giảng viên mới được đóng góp tài liệu nghiên cứu khoa học.");
        }

        var (thanhCong, thongBao, duongDan) = await _tepLuuTru.LuuTepTaiLieuAsync(model.TepTaiLieu!, "nckh");
        if (!thanhCong || duongDan is null)
        {
            return KetQua.Loi(thongBao);
        }

        var maTaiLieu = MaTuDong.TiepTheo("TL", await _db.TaiLieu.Select(t => t.MaTaiLieu).ToListAsync());

        _db.TaiLieu.Add(new TaiLieu
        {
            MaTaiLieu = maTaiLieu,
            TenTaiLieu = model.TenTaiLieu.Trim(),
            TacGia = string.IsNullOrWhiteSpace(model.TacGia) ? null : model.TacGia.Trim(),
            NhaXuatBan = string.IsNullOrWhiteSpace(model.NhaXuatBan) ? null : model.NhaXuatBan.Trim(),
            NamXuatBan = model.NamXuatBan,
            DanhMuc = string.IsNullOrWhiteSpace(model.DanhMuc) ? "Bài báo NCKH" : model.DanhMuc.Trim(),
            LaTaiLieuHanChe = model.LaTaiLieuHanChe,
            GhiChu = string.IsNullOrWhiteSpace(model.TomTat) ? null : model.TomTat.Trim(),
            TrangThaiDuyet = TrangThaiDuyetTaiLieu.ChoDuyet,
            MaNguoiDang = maDocGia
        });

        var kichThuocMb = Math.Round(model.TepTaiLieu!.Length / 1024d / 1024d, 2);
        _db.TaiLieuSo.Add(new TaiLieuSo
        {
            MaTaiLieuSo = maTaiLieu,
            DuongDanFile = duongDan,
            DungLuongFile = kichThuocMb,
            DinhDang = Path.GetExtension(model.TepTaiLieu.FileName).TrimStart('.').ToUpperInvariant(),
            SoLuotTai = 0
        });

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã gửi tài liệu \"{model.TenTaiLieu}\" (mã {maTaiLieu}). Tài liệu đang chờ thủ thư kiểm duyệt.");
    }

    public async Task<List<TaiLieuDongGopViewModel>> LayCuaDocGiaAsync(string maDocGia)
    {
        var danhSach = await _db.TaiLieu
            .AsNoTracking()
            .Include(t => t.BanSo)
            .Where(t => t.MaNguoiDang == maDocGia)
            .OrderByDescending(t => t.MaTaiLieu)
            .ToListAsync();

        return danhSach.Select(t => new TaiLieuDongGopViewModel { TaiLieu = t, BanSo = t.BanSo }).ToList();
    }

    public async Task<List<TaiLieu>> LayDanhSachAsync(string? trangThai, string? tuKhoa)
    {
        var truyVan = _db.TaiLieu
            .AsNoTracking()
            .Include(t => t.BanSo)
            .Include(t => t.NguoiDang).ThenInclude(d => d!.NguoiDung)
            .Where(t => t.BanSo != null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            truyVan = truyVan.Where(t => t.TrangThaiDuyet == trangThai);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = tuKhoa.Trim();
            truyVan = truyVan.Where(t =>
                t.MaTaiLieu.Contains(tk) ||
                t.TenTaiLieu.Contains(tk) ||
                (t.TacGia != null && t.TacGia.Contains(tk)));
        }

        return await truyVan
            .OrderBy(t => t.TrangThaiDuyet == TrangThaiDuyetTaiLieu.ChoDuyet ? 0 : 1)
            .ThenByDescending(t => t.MaTaiLieu)
            .ToListAsync();
    }

    public async Task<TaiLieu?> LayTheoMaAsync(string maTaiLieu)
    {
        return await _db.TaiLieu
            .Include(t => t.BanSo)
            .Include(t => t.NguoiDang).ThenInclude(d => d!.NguoiDung)
            .FirstOrDefaultAsync(t => t.MaTaiLieu == maTaiLieu);
    }

    public async Task<KetQua> PheDuyetAsync(string maTaiLieu)
    {
        var taiLieu = await _db.TaiLieu.FirstOrDefaultAsync(t => t.MaTaiLieu == maTaiLieu);
        if (taiLieu is null) return KetQua.Loi("Không tìm thấy tài liệu.");

        if (taiLieu.TrangThaiDuyet != TrangThaiDuyetTaiLieu.ChoDuyet)
        {
            return KetQua.Loi($"Chỉ có thể phê duyệt tài liệu đang chờ kiểm duyệt (trạng thái hiện tại: {taiLieu.TrangThaiDuyet}).");
        }

        taiLieu.TrangThaiDuyet = TrangThaiDuyetTaiLieu.DaDuyet;
        taiLieu.GhiChu = null;

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã phê duyệt tài liệu \"{taiLieu.TenTaiLieu}\" vào kho tài liệu số.");
    }

    public async Task<KetQua> TuChoiAsync(string maTaiLieu, string lyDo)
    {
        if (string.IsNullOrWhiteSpace(lyDo)) return KetQua.Loi("Vui lòng nhập lý do từ chối.");

        var taiLieu = await _db.TaiLieu.FirstOrDefaultAsync(t => t.MaTaiLieu == maTaiLieu);
        if (taiLieu is null) return KetQua.Loi("Không tìm thấy tài liệu.");

        if (taiLieu.TrangThaiDuyet != TrangThaiDuyetTaiLieu.ChoDuyet)
        {
            return KetQua.Loi("Chỉ có thể từ chối tài liệu đang chờ kiểm duyệt.");
        }

        taiLieu.TrangThaiDuyet = TrangThaiDuyetTaiLieu.TuChoi;
        taiLieu.GhiChu = lyDo.Trim();

        await _db.SaveChangesAsync();
        return KetQua.Ok($"Đã từ chối tài liệu \"{taiLieu.TenTaiLieu}\".");
    }

    public async Task<int> DemChoDuyetAsync()
    {
        return await _db.TaiLieu.CountAsync(t =>
            t.BanSo != null && t.TrangThaiDuyet == TrangThaiDuyetTaiLieu.ChoDuyet);
    }
}
