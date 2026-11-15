using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Areas.ThuThu.Controllers;

/// <summary>Phê duyệt yêu cầu xin quyền đọc tài liệu số giới hạn (PH-04).</summary>
[Area("ThuThu")]
[Authorize(Policy = "ThuThu")]
public class YeuCauController : Controller
{
    private readonly IYeuCauService _yeuCau;

    public YeuCauController(IYeuCauService yeuCau) => _yeuCau = yeuCau;

    public async Task<IActionResult> Index(string? trangThai, string? tuKhoa)
    {
        ViewBag.TrangThai = trangThai;
        ViewBag.TuKhoa = tuKhoa;
        var danhSach = await _yeuCau.LayDanhSachAsync(trangThai, tuKhoa);
        return View(danhSach);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PheDuyet(string maYeuCau)
    {
        await GhiNhan(await _yeuCau.PheDuyetAsync(maYeuCau, MaThuThu()));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(string maYeuCau)
    {
        await GhiNhan(await _yeuCau.TuChoiAsync(maYeuCau, MaThuThu()));
        return RedirectToAction(nameof(Index));
    }

    private string MaThuThu() => User.MaVaiTro() ?? User.MaNguoiDung() ?? string.Empty;

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
