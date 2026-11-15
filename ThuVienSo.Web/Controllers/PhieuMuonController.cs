using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Controllers;

/// <summary>Đăng ký mượn sách vật lý và theo dõi phiếu mượn của độc giả (PH-06).</summary>
[Authorize(Policy = "DocGia")]
public class PhieuMuonController : Controller
{
    private readonly IMuonTraService _muonTra;
    private readonly ITraCuuService _traCuu;

    public PhieuMuonController(IMuonTraService muonTra, ITraCuuService traCuu)
    {
        _muonTra = muonTra;
        _traCuu = traCuu;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var danhSach = await _muonTra.LayPhieuCuaDocGiaAsync(User.MaNguoiDung()!);
        return View(danhSach);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKyMuon(string maSach, int soNgayMuon)
    {
        if (string.IsNullOrWhiteSpace(maSach))
        {
            TempData["Loi"] = "Không xác định được sách cần mượn.";
            return RedirectToAction(nameof(Index));
        }

        if (soNgayMuon <= 0)
        {
            soNgayMuon = QuyDinh.SoNgayMuon;
        }

        var ketQua = await _muonTra.DangKyMuonAsync(User.MaNguoiDung()!, maSach, soNgayMuon);
        if (ketQua.ThanhCong)
        {
            TempData["ThanhCong"] = ketQua.ThongBao;
        }
        else
        {
            TempData["Loi"] = ketQua.ThongBao;
        }

        var sach = await _traCuu.LaySachVatLiAsync(maSach);
        if (sach is not null)
        {
            return RedirectToAction("ChiTiet", "TaiLieu", new { id = sach.TaiLieu.MaTaiLieu });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(string maPhieu)
    {
        var ketQua = await _muonTra.HuyPhieuAsync(maPhieu, User.MaNguoiDung()!);
        if (ketQua.ThanhCong)
        {
            TempData["ThanhCong"] = ketQua.ThongBao;
        }
        else
        {
            TempData["Loi"] = ketQua.ThongBao;
        }

        return RedirectToAction(nameof(Index));
    }
}
