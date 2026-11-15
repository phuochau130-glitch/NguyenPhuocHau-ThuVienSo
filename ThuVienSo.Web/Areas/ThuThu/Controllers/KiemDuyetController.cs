using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Areas.ThuThu.Controllers;

/// <summary>Kiểm duyệt tài liệu nghiên cứu khoa học do độc giả đóng góp (Quy tắc 8).</summary>
[Area("ThuThu")]
[Authorize(Policy = "ThuThu")]
public class KiemDuyetController : Controller
{
    private readonly INckhService _nckh;
    private readonly IWebHostEnvironment _moiTruong;

    public KiemDuyetController(INckhService nckh, IWebHostEnvironment moiTruong)
    {
        _nckh = nckh;
        _moiTruong = moiTruong;
    }

    public async Task<IActionResult> Index(string? trangThai, string? tuKhoa)
    {
        ViewBag.TrangThai = trangThai;
        ViewBag.TuKhoa = tuKhoa;
        var danhSach = await _nckh.LayDanhSachAsync(trangThai, tuKhoa);
        return View(danhSach);
    }

    [HttpGet]
    public async Task<IActionResult> Xem(string id)
    {
        var taiLieu = await _nckh.LayTheoMaAsync(id);
        if (taiLieu?.BanSo is null) return NotFound();
        return View(taiLieu);
    }

    /// <summary>Phục vụ tệp toàn văn cho thủ thư trong lúc kiểm tra tài liệu, kể cả khi đang chờ duyệt.</summary>
    [HttpGet]
    public async Task<IActionResult> XemTep(string id)
    {
        var taiLieu = await _nckh.LayTheoMaAsync(id);
        if (taiLieu?.BanSo is null) return NotFound();

        var duongDanVatLy = Path.Combine(
            _moiTruong.WebRootPath,
            taiLieu.BanSo.DuongDanFile.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(duongDanVatLy)) return NotFound();

        var kieuNoiDung = taiLieu.BanSo.DinhDang?.ToUpperInvariant() switch
        {
            "PDF" => "application/pdf",
            "TXT" => "text/plain; charset=utf-8",
            "DOC" => "application/msword",
            "DOCX" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        return File(System.IO.File.OpenRead(duongDanVatLy), kieuNoiDung);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PheDuyet(string maTaiLieu)
    {
        await GhiNhan(await _nckh.PheDuyetAsync(maTaiLieu));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(string maTaiLieu, string? lyDo)
    {
        await GhiNhan(await _nckh.TuChoiAsync(maTaiLieu, lyDo ?? string.Empty));
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
