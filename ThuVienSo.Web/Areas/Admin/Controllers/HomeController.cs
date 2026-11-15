using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.ViewModels;

namespace ThuVienSo.Web.Areas.Admin.Controllers;

/// <summary>Bảng điều khiển giám sát hệ thống dành cho quản trị viên (PH-07).</summary>
[Area("Admin")]
[Authorize(Policy = "Admin")]
public class HomeController : Controller
{
    private readonly IQuanTriNguoiDungService _nguoiDung;
    private readonly ITheThuVienService _theThuVien;
    private readonly IMuonTraService _muonTra;
    private readonly IYeuCauService _yeuCau;
    private readonly INckhService _nckh;
    private readonly INhatKyService _nhatKy;
    private readonly IThongKeService _thongKe;

    public HomeController(
        IQuanTriNguoiDungService nguoiDung,
        ITheThuVienService theThuVien,
        IMuonTraService muonTra,
        IYeuCauService yeuCau,
        INckhService nckh,
        INhatKyService nhatKy,
        IThongKeService thongKe)
    {
        _nguoiDung = nguoiDung;
        _theThuVien = theThuVien;
        _muonTra = muonTra;
        _yeuCau = yeuCau;
        _nckh = nckh;
        _nhatKy = nhatKy;
        _thongKe = thongKe;
    }

    public async Task<IActionResult> Index()
    {
        var thongKeMuonTra = await _muonTra.ThongKeNhanhAsync();
        var theChoDuyet = await _theThuVien.LayDanhSachAsync(TrangThaiThe.ChoDuyet, null);
        var tongTaiKhoan = await _nguoiDung.DemTaiKhoanAsync();
        var taiKhoanBiKhoa = await _nguoiDung.DemTaiKhoanBiKhoaAsync();

        var model = new BangDieuKhienAdminViewModel
        {
            TongTaiKhoan = tongTaiKhoan,
            TaiKhoanBiKhoa = taiKhoanBiKhoa,
            TaiKhoanHoatDong = tongTaiKhoan - taiKhoanBiKhoa,
            TheChoDuyet = theChoDuyet.Count,
            PhieuChoDuyet = thongKeMuonTra.GetValueOrDefault("ChoDuyet"),
            PhieuQuaHan = thongKeMuonTra.GetValueOrDefault("QuaHan"),
            YeuCauChoDuyet = await _yeuCau.DemChoDuyetAsync(),
            TaiLieuChoKiemDuyet = await _nckh.DemChoDuyetAsync(),
            TongLuotDangNhap = await _nhatKy.DemLuotDangNhapAsync(),
            ThongKe = await _thongKe.ThongKeBangDieuKhienAsync(),
            DanhSachDangNhap = await _nhatKy.LayDangNhapGanNhatAsync(10)
        };

        return View(model);
    }
}
