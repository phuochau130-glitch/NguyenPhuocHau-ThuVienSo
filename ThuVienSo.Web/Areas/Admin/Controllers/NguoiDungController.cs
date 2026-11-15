using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Areas.Admin.Controllers;

/// <summary>Quản lý tài khoản người dùng và phân quyền (UC-14, PH-07).</summary>
[Area("Admin")]
[Authorize(Policy = "Admin")]
public class NguoiDungController : Controller
{
    private readonly IQuanTriNguoiDungService _nguoiDung;
    private readonly INhatKyService _nhatKy;

    public NguoiDungController(IQuanTriNguoiDungService nguoiDung, INhatKyService nhatKy)
    {
        _nguoiDung = nguoiDung;
        _nhatKy = nhatKy;
    }

    public async Task<IActionResult> Index(string? vaiTro, string? tuKhoa, string? trangThai)
    {
        var model = await _nguoiDung.LayDanhSachAsync(vaiTro, tuKhoa, trangThai);
        return View(model);
    }

    [HttpGet]
    public IActionResult Them()
    {
        return View(new ThemNguoiDungViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(ThemNguoiDungViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var ketQua = await _nguoiDung.ThemAsync(model);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongBao);
            return View(model);
        }

        TempData["ThanhCong"] = ketQua.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Sua(string id)
    {
        var model = await _nguoiDung.LayDeSuaAsync(id);
        if (model is null)
        {
            TempData["Loi"] = "Không tìm thấy tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(SuaNguoiDungViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var ketQua = await _nguoiDung.SuaAsync(model);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongBao);
            return View(model);
        }

        await _nhatKy.GhiAsync(User.MaNguoiDung(), HanhDongNhatKy.CapNhatHoSo,
            model.MaNguoiDung, $"Quản trị viên cập nhật thông tin tài khoản {model.HoTen}.",
            HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["ThanhCong"] = ketQua.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DatLaiMatKhau(string id)
    {
        var danhSach = await _nguoiDung.LayDanhSachAsync(null, id);
        var item = danhSach.DanhSach.FirstOrDefault(n => n.MaNguoiDung == id);
        if (item is null)
        {
            TempData["Loi"] = "Không tìm thấy tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        return View(new DatLaiMatKhauViewModel { MaNguoiDung = item.MaNguoiDung, HoTen = item.HoTen });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatLaiMatKhau(DatLaiMatKhauViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var ketQua = await _nguoiDung.DatLaiMatKhauAsync(model.MaNguoiDung, model.MatKhauMoi);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongBao);
            return View(model);
        }

        await _nhatKy.GhiAsync(User.MaNguoiDung(), HanhDongNhatKy.DoiMatKhau,
            model.MaNguoiDung, $"Quản trị viên đặt lại mật khẩu cho tài khoản {model.HoTen}.",
            HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["ThanhCong"] = ketQua.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(string maNguoiDung, string trangThai)
    {
        await GhiNhan(await _nguoiDung.DoiTrangThaiAsync(maNguoiDung, trangThai));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiVaiTro(string maNguoiDung, string vaiTroMoi)
    {
        await GhiNhan(await _nguoiDung.DoiVaiTroAsync(maNguoiDung, vaiTroMoi));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(string maNguoiDung)
    {
        await GhiNhan(await _nguoiDung.XoaAsync(maNguoiDung));
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
