using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Areas.ThuThu.Controllers;

/// <summary>Quản lý kho sách vật lý của thủ thư: thêm, sửa, xóa.</summary>
[Area("ThuThu")]
[Authorize(Policy = "ThuThu")]
public class SachController : Controller
{
    private readonly IQuanLySachService _quanLySach;
    private readonly IDanhMucTacGiaService _danhMucTacGia;
    private readonly IWebHostEnvironment _moiTruong;

    public SachController(
        IQuanLySachService quanLySach,
        IDanhMucTacGiaService danhMucTacGia,
        IWebHostEnvironment moiTruong)
    {
        _quanLySach = quanLySach;
        _danhMucTacGia = danhMucTacGia;
        _moiTruong = moiTruong;
    }

    public async Task<IActionResult> Index(string? tuKhoa)
    {
        ViewBag.TuKhoa = tuKhoa;
        var danhSach = await _quanLySach.LayDanhSachAsync(tuKhoa);
        return View(danhSach);
    }

    /// <summary>Trả về nội dung chi tiết để hiển thị trong hộp thoại ngay tại trang quản lý.</summary>
    [HttpGet]
    public async Task<IActionResult> Xem(string id)
    {
        var taiLieu = await _quanLySach.LayChiTietAsync(id);
        if (taiLieu is null)
        {
            return NotFound();
        }

        return PartialView("_Xem", taiLieu);
    }

    /// <summary>Phục vụ tệp đọc thử của sách vật lý để thủ thư xem trước nội dung.</summary>
    [HttpGet]
    public async Task<IActionResult> XemTep(string id)
    {
        var taiLieu = await _quanLySach.LayChiTietAsync(id);
        if (taiLieu?.BanSach is null || string.IsNullOrWhiteSpace(taiLieu.BanSach.DuongDanTepXemTruoc))
        {
            return NotFound();
        }

        var duongDanVatLy = Path.Combine(
            _moiTruong.WebRootPath,
            taiLieu.BanSach.DuongDanTepXemTruoc.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(duongDanVatLy))
        {
            return NotFound();
        }

        var kieuNoiDung = taiLieu.BanSach.DinhDangTepXemTruoc?.ToUpperInvariant() switch
        {
            "PDF" => "application/pdf",
            "TXT" => "text/plain; charset=utf-8",
            "DOC" => "application/msword",
            "DOCX" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        return File(System.IO.File.OpenRead(duongDanVatLy), kieuNoiDung);
    }

    [HttpGet]
    public async Task<IActionResult> Them()
    {
        var model = new SachVatLiFormViewModel();
        await NapDanhMucTacGiaAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(SachVatLiFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await NapDanhMucTacGiaAsync(model);
            return View(model);
        }

        var ketQua = await _quanLySach.ThemAsync(model);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongBao);
            await NapDanhMucTacGiaAsync(model);
            return View(model);
        }

        TempData["ThanhCong"] = ketQua.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Sua(string id)
    {
        var model = await _quanLySach.LayFormSuaAsync(id);
        if (model is null)
        {
            TempData["Loi"] = "Không tìm thấy sách vật lý.";
            return RedirectToAction(nameof(Index));
        }

        await NapDanhMucTacGiaAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(SachVatLiFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await NapDanhMucTacGiaAsync(model);
            return View(model);
        }

        var ketQua = await _quanLySach.SuaAsync(model);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongBao);
            await NapDanhMucTacGiaAsync(model);
            return View(model);
        }

        TempData["ThanhCong"] = ketQua.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(string maSach)
    {
        var ketQua = await _quanLySach.XoaAsync(maSach);
        TempData[ketQua.ThanhCong ? "ThanhCong" : "Loi"] = ketQua.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    private async Task NapDanhMucTacGiaAsync(SachVatLiFormViewModel model)
    {
        model.DanhSachDanhMuc = await _danhMucTacGia.LayDanhSachDanhMucAsync();
        model.DanhSachTacGia = await _danhMucTacGia.LayDanhSachTacGiaAsync();

        ThemGiaTriHienTai(model.DanhSachDanhMuc, model.DanhMuc);
        ThemGiaTriHienTai(model.DanhSachTacGia, model.TacGia);
    }

    private static void ThemGiaTriHienTai(List<string> danhSach, string? giaTri)
    {
        if (!string.IsNullOrWhiteSpace(giaTri)
            && !danhSach.Contains(giaTri, StringComparer.OrdinalIgnoreCase))
        {
            danhSach.Add(giaTri);
            danhSach.Sort(StringComparer.CurrentCultureIgnoreCase);
        }
    }
}
