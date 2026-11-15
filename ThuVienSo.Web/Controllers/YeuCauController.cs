using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Controllers;

/// <summary>Gửi và theo dõi yêu cầu xin quyền đọc tài liệu giới hạn (PH-04).</summary>
[Authorize(Policy = "DocGia")]
public class YeuCauController : Controller
{
    private readonly IYeuCauService _yeuCau;

    public YeuCauController(IYeuCauService yeuCau) => _yeuCau = yeuCau;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var danhSach = await _yeuCau.LayCuaDocGiaAsync(User.MaNguoiDung()!);
        return View(danhSach);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gui(string maTaiLieuSo, string? lyDo)
    {
        if (string.IsNullOrWhiteSpace(maTaiLieuSo))
        {
            TempData["Loi"] = "Không xác định được tài liệu cần xin quyền đọc.";
            return RedirectToAction(nameof(Index));
        }

        var ketQua = await _yeuCau.GuiYeuCauAsync(User.MaNguoiDung()!, maTaiLieuSo, lyDo);
        if (ketQua.ThanhCong)
        {
            TempData["ThanhCong"] = ketQua.ThongBao;
        }
        else
        {
            TempData["Loi"] = ketQua.ThongBao;
        }

        return RedirectToAction(nameof(Index));
    }
}
