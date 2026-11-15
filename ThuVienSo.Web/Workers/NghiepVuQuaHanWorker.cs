using ThuVienSo.Web.Services;

namespace ThuVienSo.Web.Workers;

/// <summary>
/// Tiến trình nền tự động xử lý nghiệp vụ theo thời hạn:
/// - Hủy yêu cầu giữ sách quá 7 ngày và khóa quyền đăng ký mượn 2 ngày (Quy tắc 5, 6).
/// - Mở lại quyền đăng ký mượn khi hết thời hạn khóa.
/// </summary>
public class NghiepVuQuaHanWorker : BackgroundService
{
    private static readonly TimeSpan ChuKy = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _phamViFactory;
    private readonly ILogger<NghiepVuQuaHanWorker> _nhatKy;

    public NghiepVuQuaHanWorker(IServiceScopeFactory phamViFactory, ILogger<NghiepVuQuaHanWorker> nhatKy)
    {
        _phamViFactory = phamViFactory;
        _nhatKy = nhatKy;
    }

    protected override async Task ExecuteAsync(CancellationToken dung)
    {
        while (!dung.IsCancellationRequested)
        {
            try
            {
                using var phamVi = _phamViFactory.CreateScope();
                var muonTra = phamVi.ServiceProvider.GetRequiredService<IMuonTraService>();

                var soHuy = await muonTra.XuLyQuaHanNhanSachAsync();
                var soMoKhoa = await muonTra.MoKhoaMuonHetHanAsync();

                if (soHuy > 0 || soMoKhoa > 0)
                {
                    _nhatKy.LogInformation(
                        "Xử lý nghiệp vụ quá hạn: hủy {SoHuy} phiếu giữ sách, mở khóa {SoMoKhoa} độc giả.",
                        soHuy, soMoKhoa);
                }
            }
            catch (Exception loi)
            {
                _nhatKy.LogError(loi, "Lỗi khi xử lý nghiệp vụ quá hạn.");
            }

            try
            {
                await Task.Delay(ChuKy, dung);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
