using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Controllers;

/// <summary>Quản lý bộ sưu tập tài liệu cá nhân của độc giả.</summary>
[Authorize(Policy = "DocGia")]
public class TuSachController : Controller
{
    private readonly ThuVienDbContext _db;

    public TuSachController(ThuVienDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var maDocGia = User.MaNguoiDung()!;
        var danhSach = await _db.TaiLieuTuSach
            .AsNoTracking()
            .Where(x => x.MaDocGia == maDocGia
                && x.TaiLieu.TrangThaiDuyet == TrangThaiDuyetTaiLieu.DaDuyet)
            .Include(x => x.TaiLieu).ThenInclude(t => t.BanSach)
            .Include(x => x.TaiLieu).ThenInclude(t => t.BanSo)
            .OrderByDescending(x => x.NgayLuu)
            .ToListAsync();

        return View(danhSach);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(string maTaiLieu, string? returnUrl)
    {
        var taiLieu = await _db.TaiLieu
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.MaTaiLieu == maTaiLieu
                && t.TrangThaiDuyet == TrangThaiDuyetTaiLieu.DaDuyet);

        if (taiLieu is null)
        {
            TempData["Loi"] = "Không tìm thấy tài liệu đã được duyệt để lưu.";
        }
        else
        {
            var maDocGia = User.MaNguoiDung()!;
            var daLuu = await _db.TaiLieuTuSach.AnyAsync(x =>
                x.MaDocGia == maDocGia && x.MaTaiLieu == maTaiLieu);

            if (daLuu)
            {
                TempData["ThanhCong"] = "Tài liệu này đã có trong tủ sách cá nhân.";
            }
            else
            {
                _db.TaiLieuTuSach.Add(new TaiLieuTuSach
                {
                    MaDocGia = maDocGia,
                    MaTaiLieu = maTaiLieu,
                    NgayLuu = DateTime.Now
                });
                await _db.SaveChangesAsync();
                TempData["ThanhCong"] = "Đã lưu tài liệu vào tủ sách cá nhân.";
            }
        }

        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BoLuu(string maTaiLieu)
    {
        var maDocGia = User.MaNguoiDung()!;
        var mucLuu = await _db.TaiLieuTuSach.FirstOrDefaultAsync(x =>
            x.MaDocGia == maDocGia && x.MaTaiLieu == maTaiLieu);

        if (mucLuu is not null)
        {
            _db.TaiLieuTuSach.Remove(mucLuu);
            await _db.SaveChangesAsync();
            TempData["ThanhCong"] = "Đã bỏ tài liệu khỏi tủ sách cá nhân.";
        }

        return RedirectToAction(nameof(Index));
    }
}
