using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using todolist.Components;
using todolist.Data;
using todolist.Hubs;
using todolist.Services;

namespace todolist
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure QuestPDF License before app initialization
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Register Controllers support for AccountController
            builder.Services.AddControllersWithViews();

            builder.Services.AddServerSideBlazor()
                .AddHubOptions(options =>
                {
                    options.MaximumReceiveMessageSize = 1024 * 1024 * 5; // 5 MB max receive size
                });

            // Database Context Setup
            builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(30);
                    }
                )
            );

            // Register Custom Services
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<TaskService>();
            builder.Services.AddScoped<DepartmentService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<NotificationService>();

            // Publisher for server-side real-time delivery to Blazor components
            builder.Services.AddSingleton<NotificationPublisher>();
            builder.Services.AddHttpClient<AiService>();

            // SignalR Setup & Custom User ID Provider Registration
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

            // Authentication & Authorization Setup
            builder.Services.AddAuthenticationCore();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignOutScheme = IdentityConstants.ApplicationScheme;
            })
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/Account/Logout";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.Cookie.Name = ".TaskFlow.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            // Required to serve dynamically uploaded files from wwwroot/uploads
            app.UseStaticFiles();

            // Enable Authentication & Authorization Middleware
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            app.MapStaticAssets();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // SignalR Hub Endpoint
            app.MapHub<NotificationHub>("/notificationHub");

            // Map routes for MVC Controllers (enables AccountController.cs)
            app.MapControllers();

            // Logout Minimal API Endpoint
            app.MapPost("/Account/Logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return Results.Redirect("/login");
            });

            app.Run();
        }
    }

    // Custom UserIdProvider defined outside Main method
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? connection.User?.FindFirst("sub")?.Value
                ?? connection.User?.Identity?.Name;
        }
    }
}