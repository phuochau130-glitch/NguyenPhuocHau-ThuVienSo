using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Areas.ThuThu.Controllers;

/// <summary>Quản lý danh mục và danh sách tác giả dùng chung.</summary>
[Area("ThuThu")]
[Authorize(Policy = "ThuThu")]
public class DanhMucTacGiaController : Controller
{
    private readonly ThuVienDbContext _db;
    private readonly IDanhMucTacGiaService _quanLy;

    public DanhMucTacGiaController(ThuVienDbContext db, IDanhMucTacGiaService quanLy)
    {
        _db = db;
        _quanLy = quanLy;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new QuanLyDanhMucTacGiaViewModel
        {
            DanhMuc = await _db.DanhMucTaiLieu.AsNoTracking().OrderBy(x => x.TenDanhMuc).ToListAsync(),
            TacGia = await _db.TacGiaTaiLieu.AsNoTracking().OrderBy(x => x.TenTacGia).ToListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemDanhMuc(string ten)
    {
        DatThongBao(await _quanLy.ThemDanhMucAsync(ten));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaDanhMuc(int id, string ten)
    {
        DatThongBao(await _quanLy.SuaDanhMucAsync(id, ten));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaDanhMuc(int id)
    {
        DatThongBao(await _quanLy.XoaDanhMucAsync(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemTacGia(string ten)
    {
        DatThongBao(await _quanLy.ThemTacGiaAsync(ten));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaTacGia(int id, string ten)
    {
        DatThongBao(await _quanLy.SuaTacGiaAsync(id, ten));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaTacGia(int id)
    {
        DatThongBao(await _quanLy.XoaTacGiaAsync(id));
        return RedirectToAction(nameof(Index));
    }

    private void DatThongBao(KetQua ketQua) =>
        TempData[ketQua.ThanhCong ? "ThanhCong" : "Loi"] = ketQua.ThongBao;
}
