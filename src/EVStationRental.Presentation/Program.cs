using System.Text;
using EVStationRental.Repositories.DBContext;
using EVStationRental.Repositories.IRepositories;
using EVStationRental.Repositories.Repositories;
using EVStationRental.Repositories.UnitOfWork;
using EVStationRental.Presentation.Hubs;
using EVStationRental.Presentation.Services;
using EVStationRental.Services.ExternalService.IServices;
using EVStationRental.Services.ExternalService.Services;
using EVStationRental.Services.InternalServices.IServices.IAccountServices;
using EVStationRental.Services.InternalServices.IServices.IAuthServices;
using EVStationRental.Services.InternalServices.IServices.IDamageReportServices;
using EVStationRental.Services.InternalServices.IServices.IDashboardServices;
using EVStationRental.Services.InternalServices.IServices.IFeedbackServices;
using EVStationRental.Services.InternalServices.IServices.IOrderServices;
using EVStationRental.Services.InternalServices.IServices.IPaymentServices;
using EVStationRental.Services.InternalServices.IServices.IPromotionServices;
using EVStationRental.Services.InternalServices.IServices.IReportServices;
using EVStationRental.Services.InternalServices.IServices.IRolesServices;
using EVStationRental.Services.InternalServices.IServices.IStationServices;
using EVStationRental.Services.InternalServices.IServices.IVehicleServices;
using EVStationRental.Services.InternalServices.IServices.IWalletServices;
using EVStationRental.Services.InternalServices.Services.AccountServices;
using EVStationRental.Services.InternalServices.Services.AuthServices;
using EVStationRental.Services.InternalServices.Services.DamageReportServices;
using EVStationRental.Services.InternalServices.Services.DashboardServices;
using EVStationRental.Services.InternalServices.Services.FeedbackServices;
using EVStationRental.Services.InternalServices.Services.OrderServices;
using EVStationRental.Services.InternalServices.Services.PaymentServices;
using EVStationRental.Services.InternalServices.Services.PromotionServices;
using EVStationRental.Services.InternalServices.Services.ReportServices;
using EVStationRental.Services.InternalServices.Services.RoleServices;
using EVStationRental.Services.InternalServices.Services.StationServices;
using EVStationRental.Services.InternalServices.Services.VehicleServices;
using EVStationRental.Services.InternalServices.Services.WalletServices;
using EVStationRental.Services.Realtime;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===== PostgreSQL enum mapping =====
try
{
    Npgsql.NpgsqlConnection.GlobalTypeMapper.Reset();
    Npgsql.NpgsqlConnection.GlobalTypeMapper.MapEnum<EVStationRental.Common.Enums.EnumModel.OrderStatus>("order_status",
        nameTranslator: new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
    Npgsql.NpgsqlConnection.GlobalTypeMapper.MapEnum<EVStationRental.Common.Enums.EnumModel.VehicleStatus>("vehicle_status",
        nameTranslator: new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
    Npgsql.NpgsqlConnection.GlobalTypeMapper.MapEnum<EVStationRental.Common.Enums.EnumModel.PaymentType>("payment_type_enum",
        nameTranslator: new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
    Npgsql.NpgsqlConnection.GlobalTypeMapper.MapEnum<EVStationRental.Common.Enums.EnumModel.TransactionType>("transaction_type_enum",
        nameTranslator: new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
}
catch (Exception ex)
{
    Console.WriteLine($"[ENUM MAPPING ERROR] {ex.Message}");
}

// ===== Infrastructure & DbContext =====
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<UnitOfWork>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleModelRepository, VehicleModelRepository>();
builder.Services.AddScoped<IVehicleTypeRepository, VehicleTypeRepository>();
builder.Services.AddScoped<IStationRepository, StationRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IDamageReportRepository, DamageReportRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();

builder.Services.AddDbContext<ElectricVehicleDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Domain services =====
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IVehicleModelService, VehicleModelService>();
builder.Services.AddScoped<IVehicleTypeServices, VehicleTypeServices>();
builder.Services.AddScoped<IStationService, StationService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDamageReportService, DamageReportService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IRolesServices, RoleServices>();
builder.Services.AddScoped<IVNPayService, VNPayService>();
builder.Services.AddScoped<DatabasePaymentService>();
builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();

// ===== Razor Pages + Antiforgery =====
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AuthorizeFolder("/Staff", "StaffOrAdmin");
});
builder.Services.AddMvc(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// ===== Cookie Authentication =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Staff", "Admin"));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<RealtimeHub>("/hubs/realtime");

app.Run();
