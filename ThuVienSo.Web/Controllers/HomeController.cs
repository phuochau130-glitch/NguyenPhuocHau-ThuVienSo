using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Controllers;

/// <summary>Trang chủ công khai của thư viện số.</summary>
public class HomeController : Controller
{
    private readonly ITraCuuService _traCuu;

    public HomeController(ITraCuuService traCuu) => _traCuu = traCuu;

    public async Task<IActionResult> Index()
    {
        var tatCa = await _traCuu.TimKiemAsync(null, null, null, false);

        var model = new TrangChuViewModel
        {
            DanhSachDanhMuc = await _traCuu.LayDanhMucAsync(),
            TaiLieuNoiBat = tatCa
                .OrderByDescending(t => t.SoLuotXem)
                .ThenByDescending(t => t.NamXuatBan ?? 0)
                .Take(8)
                .ToList()
        };

        return View(model);
    }

    [AllowAnonymous]
    public IActionResult GioiThieu() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
