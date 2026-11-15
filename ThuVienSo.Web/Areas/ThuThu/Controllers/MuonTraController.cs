using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Areas.ThuThu.Controllers;

/// <summary>Nghiệp vụ mượn, trả sách và xử lý phạt (PH-06).</summary>
[Area("ThuThu")]
[Authorize(Policy = "ThuThu")]
public class MuonTraController : Controller
{
    private readonly IMuonTraService _muonTra;
    private readonly INhatKyService _nhatKy;

    public MuonTraController(IMuonTraService muonTra, INhatKyService nhatKy)
    {
        _muonTra = muonTra;
        _nhatKy = nhatKy;
    }

    public async Task<IActionResult> Index(string? trangThai, string? tuKhoa)
    {
        ViewBag.TrangThai = trangThai;
        ViewBag.TuKhoa = tuKhoa;
        var danhSach = await _muonTra.LayDanhSachAsync(trangThai, tuKhoa);
        return View(danhSach);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DuyetGiu(string maPhieu)
    {
        await GhiNhan(await _muonTra.DuyetGiuSachAsync(maPhieu, MaThuThu()),
            HanhDongNhatKy.MuonSach, maPhieu, $"Duyệt giữ sách cho phiếu {maPhieu}");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(string maPhieu, string? lyDo)
    {
        await GhiNhan(await _muonTra.TuChoiPhieuAsync(maPhieu, MaThuThu(), lyDo ?? string.Empty),
            HanhDongNhatKy.MuonSach, maPhieu, $"Từ chối phiếu mượn {maPhieu}: {lyDo}");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GiaoSach(string maPhieu)
    {
        await GhiNhan(await _muonTra.GiaoSachAsync(maPhieu, MaThuThu()),
            HanhDongNhatKy.MuonSach, maPhieu, $"Giao sách cho phiếu {maPhieu}");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NhanTra(string maPhieu, decimal phiHuHong, decimal? phiTraTre)
    {
        await GhiNhan(await _muonTra.NhanTraSachAsync(maPhieu, MaThuThu(), phiHuHong, phiTraTre),
            HanhDongNhatKy.TraSach, maPhieu,
            $"Nhận trả sách phiếu {maPhieu} (phạt hư hỏng: {phiHuHong:N0} VNĐ, phạt trả trễ: {phiTraTre:N0} VNĐ)");
        return RedirectToAction(nameof(Index));
    }

    private string MaThuThu() => User.MaVaiTro() ?? User.MaNguoiDung() ?? string.Empty;

    private async Task GhiNhan(KetQua ketQua, string hanhDong, string doiTuong, string? moTa = null)
    {
        if (ketQua.ThanhCong)
        {
            TempData["ThanhCong"] = ketQua.ThongBao;
            await _nhatKy.GhiAsync(MaThuThu(), hanhDong, doiTuong, moTa ?? ketQua.ThongBao, DiaChiIp());
        }
        else
        {
            TempData["Loi"] = ketQua.ThongBao;
        }
    }

    private string? DiaChiIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
