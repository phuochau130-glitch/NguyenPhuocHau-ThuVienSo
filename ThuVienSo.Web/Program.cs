using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ThuVienSo.Web.Data;
using ThuVienSo.Web.Models;
using ThuVienSo.Web.Services;
using ThuVienSo.Web.Workers;

var builder = WebApplication.CreateBuilder(args);

// Tầng dữ liệu
var chuoiKetNoi = builder.Configuration.GetConnectionString("ThuVienSo")
    ?? throw new InvalidOperationException("Thiếu chuỗi kết nối 'ThuVienSo' trong cấu hình.");
builder.Services.AddDbContext<ThuVienDbContext>(tuyChon => tuyChon.UseSqlServer(chuoiKetNoi));

// Tầng nghiệp vụ
builder.Services.AddSingleton<IMatKhauHasher, MatKhauHasher>();
builder.Services.AddScoped<ITepLuuTru, TepLuuTru>();
builder.Services.AddScoped<ITaiKhoanService, TaiKhoanService>();
builder.Services.AddScoped<ITraCuuService, TraCuuService>();
builder.Services.AddScoped<ITheThuVienService, TheThuVienService>();
builder.Services.AddScoped<IMuonTraService, MuonTraService>();
builder.Services.AddScoped<IYeuCauService, YeuCauService>();
builder.Services.AddScoped<INckhService, NckhService>();
builder.Services.AddScoped<IQuanLySachService, QuanLySachService>();
builder.Services.AddScoped<IQuanLyTaiLieuSoService, QuanLyTaiLieuSoService>();
builder.Services.AddScoped<IDanhMucTacGiaService, DanhMucTacGiaService>();
builder.Services.AddScoped<IThongKeService, ThongKeService>();
builder.Services.AddScoped<IQuanTriNguoiDungService, QuanTriNguoiDungService>();
builder.Services.AddScoped<INhatKyService, NhatKyService>();

// Tiến trình nền xử lý nghiệp vụ theo thời hạn
builder.Services.AddHostedService<NghiepVuQuaHanWorker>();

builder.Services.AddControllersWithViews();

// Xác thực bằng cookie và phân quyền theo vai trò (PH-01)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(tuyChon =>
    {
        tuyChon.LoginPath = "/Account/DangNhap";
        tuyChon.LogoutPath = "/Account/DangXuat";
        tuyChon.AccessDeniedPath = "/Account/KhongCoQuyen";
        tuyChon.ExpireTimeSpan = TimeSpan.FromHours(8);
        tuyChon.SlidingExpiration = true;
        tuyChon.Cookie.Name = "ThuVienSo.XacThuc";
        tuyChon.Cookie.HttpOnly = true;
    });

builder.Services.AddAuthorization(tuyChon =>
{
    tuyChon.AddPolicy("DocGia", chinhSach => chinhSach.RequireRole(VaiTro.SinhVien, VaiTro.GiangVien));
    tuyChon.AddPolicy("ThuThu", chinhSach => chinhSach.RequireRole(VaiTro.ThuThu));
    tuyChon.AddPolicy("Admin", chinhSach => chinhSach.RequireRole(VaiTro.Admin));
});

var app = builder.Build();

// Chuẩn hoá dữ liệu mẫu khi khởi động
using (var phamVi = app.Services.CreateScope())
{
    var nhatKy = phamVi.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("KhoiDong");
    var db = phamVi.ServiceProvider.GetRequiredService<ThuVienDbContext>();
    var bamMatKhau = phamVi.ServiceProvider.GetRequiredService<IMatKhauHasher>();
    try
    {
        await NangCapCsdl.ThucThiAsync(db, nhatKy);
        await DbInitializer.ChuanHoaDuLieuMauAsync(db, bamMatKhau, nhatKy);
    }
    catch (Exception loi)
    {
        nhatKy.LogError(loi, "Không thể kết nối hoặc chuẩn hoá cơ sở dữ liệu ThuVienSo.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    // NCKH files must pass through authorization-aware document endpoints.
    if (context.Request.Path.StartsWithSegments("/uploads/nckh", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
