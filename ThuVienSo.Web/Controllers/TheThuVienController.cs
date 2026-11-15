using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Controllers;

/// <summary>Đăng ký và theo dõi thẻ thư viện của độc giả (PH-03).</summary>
[Authorize(Policy = "DocGia")]
public class TheThuVienController : Controller
{
    private readonly ITheThuVienService _theThuVien;
    private readonly ITaiKhoanService _taiKhoan;
    private readonly INhatKyService _nhatKy;

    public TheThuVienController(
        ITheThuVienService theThuVien,
        ITaiKhoanService taiKhoan,
        INhatKyService nhatKy)
    {
        _theThuVien = theThuVien;
        _taiKhoan = taiKhoan;
        _nhatKy = nhatKy;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var maNguoiDung = User.MaNguoiDung()!;
        var the = await _theThuVien.LayTheCuaDocGiaAsync(maNguoiDung);
        var hoSo = await _taiKhoan.LayHoSoAsync(maNguoiDung);

        return View(new TrangThaiTheViewModel
        {
            DaCoThe = the is not null,
            The = the,
            KhoaMuonDen = hoSo?.KhoaMuonDen
        });
    }

    [HttpGet]
    public async Task<IActionResult> DangKy()
    {
        var maNguoiDung = User.MaNguoiDung()!;
        var the = await _theThuVien.LayTheCuaDocGiaAsync(maNguoiDung);

        if (the is not null && !the.DaHetHan && the.TrangThai != ThuVienSo.Web.Models.TrangThaiThe.TuChoi)
        {
            TempData["Loi"] = "Bạn đã có thẻ thư viện. Vui lòng kiểm tra trạng thái thẻ.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["DangKyLai"] = the is not null;
        ViewData["MaTheCu"] = the?.MaThe;
        ViewData["TrangThaiTheCu"] = the?.TrangThai;
        ViewData["GhiChuTheCu"] = the?.GhiChu;

        var hoSo = await _taiKhoan.LayHoSoAsync(maNguoiDung);
        return View(new DangKyTheViewModel
        {
            HoTen = hoSo?.HoTen ?? string.Empty,
            Email = hoSo?.Email ?? string.Empty,
            SoDienThoai = hoSo?.SoDienThoai ?? string.Empty,
            MaSinhVienHoacCanBo = hoSo?.MaSinhVienHoacCanBo ?? string.Empty,
            Khoa = hoSo?.Khoa
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKy(DangKyTheViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await GanThongTinDangKyLaiAsync();
            return View(model);
        }

        var ketQua = await _theThuVien.DangKyTheAsync(User.MaNguoiDung()!, model);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongBao);
            await GanThongTinDangKyLaiAsync();
            return View(model);
        }

        TempData["ThanhCong"] = ketQua.ThongBao;
        return RedirectToAction(nameof(ThanhToan));
    }

    /// <summary>Trang thanh toán lệ phí cấp thẻ thư viện (mô phỏng).</summary>
    [HttpGet]
    public async Task<IActionResult> ThanhToan()
    {
        var the = await _theThuVien.LayTheCuaDocGiaAsync(User.MaNguoiDung()!);
        if (the is null)
        {
            TempData["Loi"] = "Bạn chưa có hồ sơ thẻ thư viện để thanh toán.";
            return RedirectToAction(nameof(Index));
        }

        return View(the);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanThanhToan()
    {
        var ketQua = await _theThuVien.ThanhToanTheAsync(User.MaNguoiDung()!);
        TempData[ketQua.ThanhCong ? "ThanhCong" : "Loi"] = ketQua.ThongBao;
        return RedirectToAction(nameof(Index));
    }

    private async Task GanThongTinDangKyLaiAsync()
    {
        var the = await _theThuVien.LayTheCuaDocGiaAsync(User.MaNguoiDung()!);
        ViewData["DangKyLai"] = the is not null;
        ViewData["MaTheCu"] = the?.MaThe;
        ViewData["TrangThaiTheCu"] = the?.TrangThai;
        ViewData["GhiChuTheCu"] = the?.GhiChu;
    }

    /// <summary>Biểu mẫu chỉnh sửa thông tin cá nhân gắn với thẻ thư viện.</summary>
    [HttpGet]
    public async Task<IActionResult> ChinhSua()
    {
        var maNguoiDung = User.MaNguoiDung()!;
        var the = await _theThuVien.LayTheCuaDocGiaAsync(maNguoiDung);

        if (the is null)
        {
            TempData["Loi"] = "Bạn chưa có thẻ thư viện để chỉnh sửa thông tin.";
            return RedirectToAction(nameof(Index));
        }

        return View(new ChinhSuaTheViewModel
        {
            MaThe = the.MaThe,
            HoTen = the.DocGia.NguoiDung.HoTen,
            Email = the.DocGia.NguoiDung.Email,
            SoDienThoai = the.DocGia.NguoiDung.SoDienThoai ?? string.Empty,
            MaSinhVienHoacCanBo = the.DocGia.MaSinhVienHoacCanBo,
            Khoa = the.DocGia.Khoa
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChinhSua(ChinhSuaTheViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ketQua = await _theThuVien.CapNhatThongTinDocGiaAsync(User.MaNguoiDung()!, model);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongBao);
            return View(model);
        }

        var maNguoiDung = User.MaNguoiDung()!;
        await _nhatKy.GhiAsync(maNguoiDung, HanhDongNhatKy.CapNhatHoSo,
            maNguoiDung, "Độc giả cập nhật thông tin cá nhân từ biểu mẫu thẻ thư viện.",
            HttpContext.Connection.RemoteIpAddress?.ToString());
        TempData["ThanhCong"] = ketQua.ThongBao;
        return RedirectToAction(nameof(Index));
    }
}
