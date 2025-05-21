using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SupplyChainAPI;
using SupplyChainData;
using AutoMapper;
using NeedlRecuperatorWebApi;
using System.Globalization;


namespace NeedlRecuperatorCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            //��������� ����������� � ��
            builder.Services.AddDbContext<SupplyChainContext>(options => options.UseSqlite("Data Source = SupplyChainContext.db"));

            //������ �����������
            /*builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => options.LoginPath = "/Auth");
            builder.Services.AddAuthorization();*/

            //������ � ��������
            builder.Services.AddDistributedMemoryCache();  // ��� �������� ������ ������ � ������
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);  // ���������� ����� ����� ������ (��������, 30 �����)
                options.Cookie.HttpOnly = true;  // ��������� ������������ ��� ����
                options.Cookie.IsEssential = true;  // ���������, ��� ������ �����������
            });

            //AutoMapper configuration
            var mapper = new MapperConfiguration(mc => mc.AddProfile<MapperProfile>())
                .CreateMapper();

            builder.Services.AddSingleton(mapper);

            /* builder.Services.AddSingleton<SingletonService>();
             builder.Services.AddScoped<ScopedService>();
             builder.Services.AddTransient<TransientService>();
             builder.Services.AddTransient<Transient2Service>();*/

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseSession();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
