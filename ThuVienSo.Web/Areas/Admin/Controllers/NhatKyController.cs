using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Areas.Admin.Controllers;

/// <summary>Nhật ký hoạt động hệ thống dành cho quản trị viên (PH-07).</summary>
[Area("Admin")]
[Authorize(Policy = "Admin")]
public class NhatKyController : Controller
{
    private readonly INhatKyService _nhatKy;

    public NhatKyController(INhatKyService nhatKy) => _nhatKy = nhatKy;

    public async Task<IActionResult> Index(string? hanhDong, string? tuKhoa, DateTime? tuNgay, DateTime? denNgay)
    {
        var model = await _nhatKy.LayDanhSachAsync(hanhDong, tuKhoa, tuNgay, denNgay);
        return View(model);
    }
}
