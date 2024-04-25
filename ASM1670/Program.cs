using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ASM1670.Data;
using ASM1670.Models;
using System.Configuration;
using ASM1670.Hubs;
using ASM1670.Controllers;
var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("ASM1670ContextConnection") ?? throw new InvalidOperationException("Connection string 'ASM1670ContextConnection' not found.");

builder.Services.AddDbContext<ASM1670Context>(options => options.UseSqlServer(connectionString));

builder.Services.AddSingleton<TwilioService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var accountSid = configuration["Twilio:AccountSID"];
    var authToken = configuration["Twilio:AuthToken"];
    var twilioPhoneNumber = configuration["Twilio:PhoneNumber"];

    return new TwilioService(accountSid, authToken, twilioPhoneNumber);
});
//builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true).AddDefaultTokenProviders()
//    .AddRoles<IdentityRole>().AddEntityFrameworkStores<ASM1670Context>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddEntityFrameworkStores<ASM1670Context>()
        .AddDefaultUI()
        .AddDefaultTokenProviders();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.Configure<IdentityOptions>(options =>
{
    // Default Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.MapHub<NotificationHub>("/notificationHub");


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();
app.MapRazorPages();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
