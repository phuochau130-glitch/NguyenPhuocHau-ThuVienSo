using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Areas.ThuThu.Controllers;

/// <summary>Quản lý kho tài liệu số của thủ thư: thêm, sửa, xóa.</summary>
[Area("ThuThu")]
[Authorize(Policy = "ThuThu")]
public class TaiLieuSoController : Controller
{
    private readonly IQuanLyTaiLieuSoService _quanLyTaiLieuSo;
    private readonly IDanhMucTacGiaService _danhMucTacGia;
    private readonly IWebHostEnvironment _moiTruong;

    public TaiLieuSoController(
        IQuanLyTaiLieuSoService quanLyTaiLieuSo,
        IDanhMucTacGiaService danhMucTacGia,
        IWebHostEnvironment moiTruong)
    {
        _quanLyTaiLieuSo = quanLyTaiLieuSo;
        _danhMucTacGia = danhMucTacGia;
        _moiTruong = moiTruong;
    }

    public async Task<IActionResult> Index(string? tuKhoa)
    {
        ViewBag.TuKhoa = tuKhoa;
        var danhSach = await _quanLyTaiLieuSo.LayDanhSachAsync(tuKhoa);
        return View(danhSach);
    }

    /// <summary>Trả về nội dung chi tiết để hiển thị trong hộp thoại ngay tại trang quản lý.</summary>
    [HttpGet]
    public async Task<IActionResult> Xem(string id)
    {
        var taiLieu = await _quanLyTaiLieuSo.LayChiTietAsync(id);
        if (taiLieu is null)
        {
            return NotFound();
        }

        return PartialView("_Xem", taiLieu);
    }

    /// <summary>Phục vụ tệp tài liệu số để thủ thư xem trước nội dung.</summary>
    [HttpGet]
    public async Task<IActionResult> XemTep(string id)
    {
        var taiLieu = await _quanLyTaiLieuSo.LayChiTietAsync(id);
        if (taiLieu?.BanSo is null || string.IsNullOrWhiteSpace(taiLieu.BanSo.DuongDanFile))
        {
            return NotFound();
        }

        var duongDanVatLy = Path.Combine(
            _moiTruong.WebRootPath,
            taiLieu.BanSo.DuongDanFile.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(duongDanVatLy))
        {
            return NotFound();
        }

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

    [HttpGet]
    public async Task<IActionResult> Them()
    {
        var model = new TaiLieuSoFormViewModel();
        await NapDanhMucTacGiaAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(TaiLieuSoFormViewModel model)
    {
        if (model.TepTaiLieu is null || model.TepTaiLieu.Length == 0)
        {
            ModelState.AddModelError(nameof(model.TepTaiLieu), "Vui lòng chọn tệp tài liệu để tải lên.");
        }

        if (!ModelState.IsValid)
        {
            await NapDanhMucTacGiaAsync(model);
            return View(model);
        }

        var ketQua = await _quanLyTaiLieuSo.ThemAsync(model, _moiTruong.WebRootPath);
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
        var model = await _quanLyTaiLieuSo.LayFormSuaAsync(id);
        if (model is null)
        {
            TempData["Loi"] = "Không tìm thấy tài liệu số.";
            return RedirectToAction(nameof(Index));
        }

        await NapDanhMucTacGiaAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(TaiLieuSoFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await NapDanhMucTacGiaAsync(model);
            return View(model);
        }

        var ketQua = await _quanLyTaiLieuSo.SuaAsync(model, _moiTruong.WebRootPath);
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
    public async Task<IActionResult> Xoa(string maTaiLieuSo)
    {
        var ketQua = await _quanLyTaiLieuSo.XoaAsync(maTaiLieuSo, _moiTruong.WebRootPath);
        TempData[ketQua.ThanhCong ? "ThanhCong" : "Loi"] = ketQua.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    private async Task NapDanhMucTacGiaAsync(TaiLieuSoFormViewModel model)
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
