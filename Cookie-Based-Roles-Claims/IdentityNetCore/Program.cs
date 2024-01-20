using System;
using IdentityNetCore.Data;
using IdentityNetCore.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityNetCore;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        var connString = Configuration["ConnectionStrings:Default"];
        services.AddDbContext<ApplicationDBContext>(o => o.UseSqlServer(connString));
        services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDBContext>();

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequiredLength = 3;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = false;

            options.Lockout.MaxFailedAccessAttempts = 3;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

            options.SignIn.RequireConfirmedEmail = false;
        });

        services.ConfigureApplicationCookie(option =>
        {
            option.LoginPath = "/Identity/Signin";
            option.AccessDeniedPath = "/Identity/AccessDenied";
            option.ExpireTimeSpan = TimeSpan.FromHours(10);
        });

        services.Configure<SmtpOptions>(Configuration.GetSection("Smtp"));

        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddAuthorization(option =>
        {
            option.AddPolicy("MemberDep", p => { p.RequireClaim("Department", "Tech").RequireRole("Member"); });

            option.AddPolicy("AdminDep", p => { p.RequireClaim("Department", "Tech").RequireRole("Admin"); });
        });
        services.AddControllersWithViews();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();
        else
            app.UseExceptionHandler("/Home/Error");
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                "default",
                "{controller=Home}/{action=Index}");
        });
    }
}