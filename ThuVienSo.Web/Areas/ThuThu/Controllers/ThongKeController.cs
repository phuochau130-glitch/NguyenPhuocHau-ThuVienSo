using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Areas.ThuThu.Controllers;

/// <summary>Thống kê hoạt động thư viện và xuất báo cáo Excel (PH-08).</summary>
[Area("ThuThu")]
[Authorize(Policy = "ThuThu")]
public class ThongKeController : Controller
{
    private readonly IThongKeService _thongKe;

    public ThongKeController(IThongKeService thongKe) => _thongKe = thongKe;

    public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay, string? loaiBaoCao)
    {
        var baoCao = await _thongKe.BaoCaoAsync(tuNgay, denNgay, loaiBaoCao);
        return View(baoCao);
    }

    /// <summary>Xuất báo cáo thống kê ra tệp Excel (.xlsx) theo bộ lọc đang chọn.</summary>
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
