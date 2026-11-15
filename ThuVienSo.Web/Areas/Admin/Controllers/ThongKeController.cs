using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Areas.Admin.Controllers;

/// <summary>Giám sát thống kê toàn hệ thống (PH-08).</summary>
[Area("Admin")]
[Authorize(Policy = "Admin")]
public class ThongKeController : Controller
{
    private readonly IThongKeService _thongKe;

    public ThongKeController(IThongKeService thongKe) => _thongKe = thongKe;

    public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay, string? loaiBaoCao)
    {
        var baoCao = await _thongKe.BaoCaoAsync(tuNgay, denNgay, loaiBaoCao);
        return View(baoCao);
    }

    public async Task<IActionResult> XuatExcel(DateTime? tuNgay, DateTime? denNgay, string? loaiBaoCao)
    {
        var baoCao = await _thongKe.BaoCaoAsync(tuNgay, denNgay, loaiBaoCao);
        var noiDung = _thongKe.XuatExcel(baoCao);
        var tenTep = $"BaoCaoThongKe_{DateTime.Now:yyyyMMddHHmm}.xlsx";
        return File(noiDung,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            tenTep);
    }
}
