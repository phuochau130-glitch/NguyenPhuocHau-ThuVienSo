using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Data;

/// <summary>
/// Chuẩn hoá dữ liệu mẫu sau khi khởi tạo cơ sở dữ liệu:
/// băm lại mật khẩu dữ liệu mẫu để không lưu mật khẩu dạng văn bản thuần.
/// </summary>
public static class DbInitializer
{
    /// <summary>Mật khẩu mặc định áp dụng cho toàn bộ tài khoản dữ liệu mẫu.</summary>
    public const string MatKhauMacDinh = "123456";

    public static async Task ChuanHoaDuLieuMauAsync(ThuVienDbContext db, IMatKhauHasher bamMatKhau, ILogger logger)
    {
        var danhSach = await db.NguoiDung.ToListAsync();
        var soCapNhat = 0;

        foreach (var nguoiDung in danhSach)
        {
            if (MatKhauHasher.DaBam(nguoiDung.MatKhau)) continue;

            nguoiDung.MatKhau = bamMatKhau.Bam(MatKhauMacDinh);
            soCapNhat++;
        }

        if (soCapNhat > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation(
                "Đã chuẩn hoá mật khẩu cho {SoLuong} tài khoản dữ liệu mẫu (mật khẩu mặc định: {MatKhau}).",
                soCapNhat, MatKhauMacDinh);
        }
    }
}
