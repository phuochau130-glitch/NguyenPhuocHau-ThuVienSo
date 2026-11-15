using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Areas.ThuThu.Controllers;

/// <summary>Quản lý và xét duyệt thẻ thư viện (PH-03, PH-05).</summary>
[Area("ThuThu")]
[Authorize(Policy = "ThuThu")]
public class TheThuVienController : Controller
{
    private readonly ITheThuVienService _theThuVien;

    public TheThuVienController(ITheThuVienService theThuVien) => _theThuVien = theThuVien;

    public async Task<IActionResult> Index(string? trangThai, string? tuKhoa, bool? daThanhToan)
    {
        ViewBag.TrangThai = trangThai;
        ViewBag.TuKhoa = tuKhoa;
        ViewBag.DaThanhToan = daThanhToan;
        var danhSach = await _theThuVien.LayDanhSachAsync(trangThai, tuKhoa, daThanhToan);
        return View(danhSach);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiThanhToan(string maThe, bool daThanhToan)
    {
        await GhiNhan(await _theThuVien.DoiThanhToanAsync(maThe, daThanhToan));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(string maThe)
    {
        await GhiNhan(await _theThuVien.DuyetTheAsync(maThe));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(string maThe, string? lyDo)
    {
        await GhiNhan(await _theThuVien.TuChoiTheAsync(maThe, lyDo ?? string.Empty));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(string maThe, string trangThai, string? ghiChu)
    {
        await GhiNhan(await _theThuVien.DoiTrangThaiAsync(maThe, trangThai, ghiChu));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(string maThe)
    {
        await GhiNhan(await _theThuVien.XoaTheAsync(maThe));
        return RedirectToAction(nameof(Index));
    }

    private Task GhiNhan(KetQua ketQua)
    {
        if (ketQua.ThanhCong)
        {
            TempData["ThanhCong"] = ketQua.ThongBao;
        }
        else
        {
            TempData["Loi"] = ketQua.ThongBao;
        }

        return Task.CompletedTask;
    }
}
