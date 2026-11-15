using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Controllers;

/// <summary>Đóng góp và theo dõi kiểm duyệt tài liệu nghiên cứu khoa học (Quy tắc 8).</summary>
[Authorize(Policy = "DocGia")]
public class NckhController : Controller
{
    private readonly INckhService _nckh;
    private readonly IWebHostEnvironment _moiTruong;

    public NckhController(INckhService nckh, IWebHostEnvironment moiTruong)
    {
        _nckh = nckh;
        _moiTruong = moiTruong;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var danhSach = await _nckh.LayCuaDocGiaAsync(User.MaNguoiDung()!);
        return View(danhSach);
    }

    [HttpGet]
    public IActionResult TaiLen() => View(new TaiLenNckhViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TaiLen(TaiLenNckhViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var ketQua = await _nckh.TaiLenAsync(User.MaNguoiDung()!, model, _moiTruong.WebRootPath);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongBao);
            return View(model);
        }

        TempData["ThanhCong"] = ketQua.ThongBao;
        return RedirectToAction(nameof(Index));
    }
}
