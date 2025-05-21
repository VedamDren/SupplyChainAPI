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

            //Настройка подключения к БД
            builder.Services.AddDbContext<SupplyChainContext>(options => options.UseSqlite("Data Source = SupplyChainContext.db"));

            //Сервис Авторизации
            /*builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => options.LoginPath = "/Auth");
            builder.Services.AddAuthorization();*/

            //Работа с сессиями
            builder.Services.AddDistributedMemoryCache();  // Для хранения данных сессии в памяти
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);  // Установите время жизни сессии (например, 30 минут)
                options.Cookie.HttpOnly = true;  // Настройка безопасности для куки
                options.Cookie.IsEssential = true;  // Убедитесь, что сессия обязательна
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
