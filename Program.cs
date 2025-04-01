using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Middleware;
using SchoolManagementSystem.Services.Implementation;
using SchoolManagementSystem.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add Distributed Memory Cache for Session state
builder.Services.AddDistributedMemoryCache();

// Add Session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Required for GDPR compliance and essential session function
});

// Register IHttpContextAccessor to access HttpContext in services
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Register SessionService as a singleton
builder.Services.AddSingleton<ISessionService>(provider =>
{
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    return SessionService.GetInstance(httpContextAccessor);
});

// Initialize UnitOfWork singleton
var csvBasePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "CSV");
UnitOfWork.GetInstance(csvBasePath);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Enable session state middleware

app.UseMiddleware<AuthenticationMiddleware>(); // Add custom authentication middleware

app.UseAuthorization(); // Enable authorization capabilities

// Map controller routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Run the application
app.Run();