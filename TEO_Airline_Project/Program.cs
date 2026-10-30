using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TEO_Airline_Project.Models;
using TEO_Airline_Project.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AirlineBookingDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ICounterTicketService, CounterTicketService>();
builder.Services.AddOptions<CounterTicketPolicyOptions>()
    .Bind(builder.Configuration.GetSection("CounterTicketPolicy"))
    .Validate(
        options => options.ExchangeSurchargePercent is > 0m and <= 100m &&
                   options.PartialCancellationRefundPercent is > 0m and <= 100m,
        "Ticket policy percentages must be greater than 0 and no more than 100.")
    .ValidateOnStart();

// Thêm dịch vụ Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian timeout session
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
