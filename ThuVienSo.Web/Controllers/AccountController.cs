using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Controllers;

/// <summary>Xác thực và quản lý tài khoản cá nhân (PH-01).</summary>
public class AccountController : Controller
{
    private readonly ITaiKhoanService _taiKhoan;
    private readonly IMuonTraService _muonTra;
    private readonly IYeuCauService _yeuCau;
    private readonly INhatKyService _nhatKy;
    private readonly ITraCuuService _traCuu;

    public AccountController(
        ITaiKhoanService taiKhoan,
        IMuonTraService muonTra,
        IYeuCauService yeuCau,
        INhatKyService nhatKy,
        ITraCuuService traCuu)
    {
        _taiKhoan = taiKhoan;
        _muonTra = muonTra;
        _yeuCau = yeuCau;
        _nhatKy = nhatKy;
        _traCuu = traCuu;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult DangNhap(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return ChuyenHuongTheoVaiTro();
        }

        return View(new DangNhapViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangNhap(DangNhapViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var nguoiDung = await _taiKhoan.DangNhapAsync(model.Email.Trim(), model.MatKhau);
        if (nguoiDung is null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác, hoặc tài khoản đang bị khóa.");
            return View(model);
        }

        var vaiTro = nguoiDung.VaiTro;
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, nguoiDung.HoTen),
            new(ClaimTypes.Role, vaiTro),
            new(NguoiDungHienTai.LoaiClaimMaNguoiDung, nguoiDung.MaNguoiDung),
            new(NguoiDungHienTai.LoaiClaimMaVaiTro, nguoiDung.MaNguoiDung)
        };

        var danhTinh = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(danhTinh),
            new AuthenticationProperties { IsPersistent = model.GhiNho });

        await _nhatKy.GhiAsync(nguoiDung.MaNguoiDung, HanhDongNhatKy.DangNhap, nguoiDung.MaNguoiDung,
            $"Đăng nhập với vai trò {VaiTro.TenHienThi(vaiTro)} ({nguoiDung.Email})", DiaChiIp());

        TempData["ThanhCong"] = $"Xin chào {nguoiDung.HoTen}! Bạn đã đăng nhập với vai trò {VaiTro.TenHienThi(vaiTro)}.";

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return ChuyenHuongTheoVaiTro(vaiTro);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangXuat()
    {
        var maNguoiDung = User.MaNguoiDung();
        var tenHienThi = User.TenHienThi();
        var ip = DiaChiIp();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!string.IsNullOrWhiteSpace(maNguoiDung))
        {
            await _nhatKy.GhiAsync(maNguoiDung, HanhDongNhatKy.DangXuat, maNguoiDung,
                $"Đăng xuất khỏi hệ thống ({tenHienThi})", ip);
        }

        TempData["ThanhCong"] = "Bạn đã đăng xuất khỏi hệ thống.";
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult KhongCoQuyen() => View();

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> HoSo()
    {
        var hoSo = await _taiKhoan.LayHoSoAsync(User.MaNguoiDung()!);
        if (hoSo is null)
        {
            TempData["Loi"] = "Không tìm thấy thông tin tài khoản.";
            return RedirectToAction("Index", "Home");
        }

        return View(hoSo);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> SuaHoSo()
    {
        var hoSo = await _taiKhoan.LayHoSoAsync(User.MaNguoiDung()!);
        if (hoSo is null)
        {
            TempData["Loi"] = "Không tìm thấy thông tin tài khoản.";
            return RedirectToAction("Index", "Home");
        }

        return View(new CapNhatHoSoViewModel
        {
            VaiTro = hoSo.VaiTro,
            HoTen = hoSo.HoTen,
            Email = hoSo.Email,
            SoDienThoai = hoSo.SoDienThoai,
            Khoa = hoSo.Khoa,
            BoPhan = hoSo.BoPhan
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaHoSo(CapNhatHoSoViewModel model)
    {
        model.VaiTro = User.VaiTro();
        if (!ModelState.IsValid) return View(model);

        var ketQua = await _taiKhoan.CapNhatHoSoAsync(User.MaNguoiDung()!, model);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongBao);
            return View(model);
        }

        var identity = (ClaimsIdentity)User.Identity!;
        var tenCu = identity.FindFirst(ClaimTypes.Name);
        if (tenCu is not null) identity.RemoveClaim(tenCu);
        identity.AddClaim(new Claim(ClaimTypes.Name, model.HoTen.Trim()));

        var phienDangNhap = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            phienDangNhap.Properties);

        await _nhatKy.GhiAsync(User.MaNguoiDung(), HanhDongNhatKy.CapNhatHoSo,
            User.MaNguoiDung(), "Người dùng tự cập nhật thông tin hồ sơ.", DiaChiIp());
        TempData["ThanhCong"] = ketQua.ThongBao;
        return RedirectToAction(nameof(HoSo));
    }

    [Authorize]
    [HttpGet]
    public IActionResult DoiMatKhau() => View(new DoiMatKhauViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var ketQua = await _taiKhoan.DoiMatKhauAsync(User.MaNguoiDung()!, model.MatKhauCu, model.MatKhauMoi);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(string.Empty, ketQua.ThongBao);
            return View(model);
        }

        await _nhatKy.GhiAsync(User.MaNguoiDung(), HanhDongNhatKy.DoiMatKhau,
            User.MaNguoiDung(), "Người dùng tự đổi mật khẩu.", DiaChiIp());
        TempData["ThanhCong"] = ketQua.ThongBao;
        return RedirectToAction(nameof(HoSo));
    }

    /// <summary>Lịch sử đọc của độc giả: phiếu mượn trả và yêu cầu đọc tài liệu số.</summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> LichSuDoc()
    {
        var maNguoiDung = User.MaNguoiDung()!;
        var phieuMuon = await _muonTra.LayPhieuCuaDocGiaAsync(maNguoiDung);
        var yeuCau = await _yeuCau.LayCuaDocGiaAsync(maNguoiDung);
        var taiLieuDaDoc = await _traCuu.LayTaiLieuDaDocAsync(maNguoiDung);

        return View(new LichSuDocViewModel
        {
            PhieuMuon = phieuMuon,
            YeuCau = yeuCau,
            TaiLieuDaDoc = taiLieuDaDoc
        });
    }

    /// <summary>Điều hướng người dùng về khu vực làm việc tương ứng với vai trò.</summary>
    private IActionResult ChuyenHuongTheoVaiTro(string? vaiTro = null)
    {
        vaiTro ??= User.VaiTro();
        return vaiTro switch
        {
            VaiTro.Admin => RedirectToAction("Index", "Home", new { area = "Admin" }),
            VaiTro.ThuThu => RedirectToAction("Index", "Home", new { area = "ThuThu" }),
            _ => RedirectToAction("Index", "Home")
        };
    }

    /// <summary>Địa chỉ IP của trình duyệt đang gửi yêu cầu (dùng cho nhật ký hoạt động).</summary>
    private string? DiaChiIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
