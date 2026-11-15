using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;

namespace ThuVienSo.Web.Services;

public interface IDanhMucTacGiaService
{
    Task<List<string>> LayDanhSachDanhMucAsync();
    Task<List<string>> LayDanhSachTacGiaAsync();
    Task<KetQua> ThemDanhMucAsync(string ten);
    Task<KetQua> SuaDanhMucAsync(int id, string ten);
    Task<KetQua> XoaDanhMucAsync(int id);
    Task<KetQua> ThemTacGiaAsync(string ten);
    Task<KetQua> SuaTacGiaAsync(int id, string ten);
    Task<KetQua> XoaTacGiaAsync(int id);
}

/// <summary>Quản lý danh mục và tác giả dùng chung trong kho tài liệu.</summary>
public class DanhMucTacGiaService : IDanhMucTacGiaService
{
    private readonly ThuVienDbContext _db;

    public DanhMucTacGiaService(ThuVienDbContext db) => _db = db;

    public Task<List<string>> LayDanhSachDanhMucAsync() =>
        _db.DanhMucTaiLieu.AsNoTracking()
            .OrderBy(x => x.TenDanhMuc)
            .Select(x => x.TenDanhMuc)
            .ToListAsync();

    public Task<List<string>> LayDanhSachTacGiaAsync() =>
        _db.TacGiaTaiLieu.AsNoTracking()
            .OrderBy(x => x.TenTacGia)
            .Select(x => x.TenTacGia)
            .ToListAsync();

    public async Task<KetQua> ThemDanhMucAsync(string ten)
    {
        ten = (ten ?? string.Empty).Trim();
        if (!HopLe(ten, 100)) return KetQua.Loi("Tên danh mục không được để trống và tối đa 100 ký tự.");
        if (await _db.DanhMucTaiLieu.AnyAsync(x => x.TenDanhMuc == ten))
            return KetQua.Loi("Danh mục này đã tồn tại.");

        _db.DanhMucTaiLieu.Add(new DanhMucTaiLieu { TenDanhMuc = ten });
        await _db.SaveChangesAsync();
        return KetQua.Ok("Đã thêm danh mục.");
    }

    public async Task<KetQua> SuaDanhMucAsync(int id, string ten)
    {
        ten = (ten ?? string.Empty).Trim();
        if (!HopLe(ten, 100)) return KetQua.Loi("Tên danh mục không được để trống và tối đa 100 ký tự.");
        var danhMuc = await _db.DanhMucTaiLieu.FirstOrDefaultAsync(x => x.MaDanhMuc == id);
        if (danhMuc is null) return KetQua.Loi("Không tìm thấy danh mục.");
        if (await _db.DanhMucTaiLieu.AnyAsync(x => x.MaDanhMuc != id && x.TenDanhMuc == ten))
            return KetQua.Loi("Danh mục này đã tồn tại.");

        var tenCu = danhMuc.TenDanhMuc;
        await _db.TaiLieu
            .Where(t => t.DanhMuc != null && t.DanhMuc.Trim().ToUpper() == tenCu.ToUpper())
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.DanhMuc, ten));
        danhMuc.TenDanhMuc = ten;
        await _db.SaveChangesAsync();
        return KetQua.Ok("Đã cập nhật danh mục và các tài liệu liên quan.");
    }

    public async Task<KetQua> XoaDanhMucAsync(int id)
    {
        var danhMuc = await _db.DanhMucTaiLieu.FirstOrDefaultAsync(x => x.MaDanhMuc == id);
        if (danhMuc is null) return KetQua.Loi("Không tìm thấy danh mục.");
        var dangSuDung = await _db.TaiLieu.AnyAsync(t =>
            t.DanhMuc != null && t.DanhMuc.Trim().ToUpper() == danhMuc.TenDanhMuc.ToUpper());
        if (dangSuDung) return KetQua.Loi("Không thể xóa danh mục đang được tài liệu sử dụng.");

        _db.DanhMucTaiLieu.Remove(danhMuc);
        await _db.SaveChangesAsync();
        return KetQua.Ok("Đã xóa danh mục.");
    }

    public async Task<KetQua> ThemTacGiaAsync(string ten)
    {
        ten = (ten ?? string.Empty).Trim();
        if (!HopLe(ten, 200)) return KetQua.Loi("Tên tác giả không được để trống và tối đa 200 ký tự.");
        if (await _db.TacGiaTaiLieu.AnyAsync(x => x.TenTacGia == ten))
            return KetQua.Loi("Tác giả này đã tồn tại.");

        _db.TacGiaTaiLieu.Add(new TacGiaTaiLieu { TenTacGia = ten });
        await _db.SaveChangesAsync();
        return KetQua.Ok("Đã thêm tác giả.");
    }

    public async Task<KetQua> SuaTacGiaAsync(int id, string ten)
    {
        ten = (ten ?? string.Empty).Trim();
        if (!HopLe(ten, 200)) return KetQua.Loi("Tên tác giả không được để trống và tối đa 200 ký tự.");
        var tacGia = await _db.TacGiaTaiLieu.FirstOrDefaultAsync(x => x.MaTacGia == id);
        if (tacGia is null) return KetQua.Loi("Không tìm thấy tác giả.");
        if (await _db.TacGiaTaiLieu.AnyAsync(x => x.MaTacGia != id && x.TenTacGia == ten))
            return KetQua.Loi("Tác giả này đã tồn tại.");

        var tenCu = tacGia.TenTacGia;
        await _db.TaiLieu
            .Where(t => t.TacGia != null && t.TacGia.Trim().ToUpper() == tenCu.ToUpper())
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.TacGia, ten));
        tacGia.TenTacGia = ten;
        await _db.SaveChangesAsync();
        return KetQua.Ok("Đã cập nhật tác giả và các tài liệu liên quan.");
    }

    public async Task<KetQua> XoaTacGiaAsync(int id)
    {
        var tacGia = await _db.TacGiaTaiLieu.FirstOrDefaultAsync(x => x.MaTacGia == id);
        if (tacGia is null) return KetQua.Loi("Không tìm thấy tác giả.");
        var dangSuDung = await _db.TaiLieu.AnyAsync(t =>
            t.TacGia != null && t.TacGia.Trim().ToUpper() == tacGia.TenTacGia.ToUpper());
        if (dangSuDung) return KetQua.Loi("Không thể xóa tác giả đang được tài liệu sử dụng.");

        _db.TacGiaTaiLieu.Remove(tacGia);
        await _db.SaveChangesAsync();
        return KetQua.Ok("Đã xóa tác giả.");
    }

    private static bool HopLe(string ten, int doDaiToiDa) =>
        !string.IsNullOrWhiteSpace(ten) && ten.Length <= doDaiToiDa;
}
