using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Areas.ThuThu.Controllers;

/// <summary>Bảng điều khiển nghiệp vụ dành cho thủ thư (PH-02).</summary>
[Area("ThuThu")]
[Authorize(Policy = "ThuThu")]
public class HomeController : Controller
{
    private readonly ITheThuVienService _theThuVien;
    private readonly IMuonTraService _muonTra;
    private readonly IYeuCauService _yeuCau;
    private readonly INckhService _nckh;
    private readonly IThongKeService _thongKe;

    public HomeController(
        ITheThuVienService theThuVien,
        IMuonTraService muonTra,
        IYeuCauService yeuCau,
        INckhService nckh,
        IThongKeService thongKe)
    {
        _theThuVien = theThuVien;
        _muonTra = muonTra;
        _yeuCau = yeuCau;
        _nckh = nckh;
        _thongKe = thongKe;
    }

    public async Task<IActionResult> Index()
    {
        var thongKeMuonTra = await _muonTra.ThongKeNhanhAsync();
        var theChoDuyet = await _theThuVien.LayDanhSachAsync(TrangThaiThe.ChoDuyet, null);

        var model = new BangDieuKhienThuThuViewModel
        {
            TheChoDuyet = theChoDuyet.Count,
            PhieuChoDuyet = thongKeMuonTra.GetValueOrDefault("ChoDuyet"),
            PhieuDangGiu = thongKeMuonTra.GetValueOrDefault("DangGiu"),
            PhieuDangMuon = thongKeMuonTra.GetValueOrDefault("DangMuon"),
            PhieuQuaHan = thongKeMuonTra.GetValueOrDefault("QuaHan"),
            YeuCauChoDuyet = await _yeuCau.DemChoDuyetAsync(),
            TaiLieuChoKiemDuyet = await _nckh.DemChoDuyetAsync(),
            ThongKe = await _thongKe.ThongKeBangDieuKhienAsync()
        };

        return View(model);
    }
}
