using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SupplyChainData;
using SupplyChainAPI.Mappings;
using SupplyChainAPI.Configuration;
using SupplyChainMathLib;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using SupplyChainAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Настройка локализации
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// Добавляем сжатие ответов для повышения производительности
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});

// Настройка подключения к БД
builder.Services.AddDbContext<SupplyChainContext>(options =>
    options.UseSqlite("Data Source = SupplyChain.db"));

// Регистрация AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Настройка JWT аутентификации
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = AuthOptions.ISSUER,
        ValidateAudience = true,
        ValidAudience = AuthOptions.AUDIENCE,
        ValidateLifetime = true,
        IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "Ошибка аутентификации JWT");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userId = context.Principal?.FindFirst("UserId")?.Value;
            logger.LogInformation("Пользователь успешно аутентифицирован. UserId: {UserId}", userId);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Ошибка авторизации: {Error}", context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

// Настройка авторизации
builder.Services.AddAuthorization();

// Регистрация сервисов и репозиториев - ОБНОВЛЯЕМ ЭТУ СТРОКУ
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value.Errors.Count > 0)
                .SelectMany(e => e.Value.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            var result = new
            {
                Message = "Ошибка валидации данных",
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };

            return new BadRequestObjectResult(result);
        };
    });

builder.Services.AddHttpContextAccessor();

// Настройка CORS для работы с фронтендом - ОБНОВЛЯЕМ
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition")
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Настройка Swagger с поддержкой JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Supply Chain API",
        Version = "v1",
        Description = "API для автоматизации планирования цепочек поставок",
        Contact = new OpenApiContact
        {
            Name = "Supply Chain Team",
            Email = "support@supplychain.com"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Регистрация сервисов расчета
builder.Services.AddScoped<InventoryCalculator>();
builder.Services.AddScoped<ProductionCalculator>();
builder.Services.AddScoped<SupplyCalculator>();
builder.Services.AddScoped<InventoryCalculatorService>();

var app = builder.Build();

// Включаем сжатие ответов
app.UseResponseCompression();

// Конфигурация конвейера запросов
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Supply Chain API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.DefaultModelExpandDepth(2);
        c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
        c.EnableDeepLinking();
        c.EnableFilter();
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Применяем CORS политику - ИСПРАВЛЯЕМ СИНТАКСИС
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Включаем маршрутизацию
app.UseRouting();

// Включаем аутентификацию и авторизацию
app.UseAuthentication();
app.UseAuthorization();

// Сопоставление контроллеров с маршрутами
app.MapControllers();

// Создаем маршрут для тестирования аутентификации
app.MapGet("/api/auth/test", () => "Аутентификация работает!")
    .RequireAuthorization();

app.MapGet("/api/public/test", () => "Публичный эндпоинт доступен без авторизации");

// Добавляем endpoint для обработки ошибок
app.Map("/error", (HttpContext context) =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetService<ILogger<Program>>();

    logger?.LogError(exception, "Необработанное исключение");

    var result = new
    {
        Title = "Внутренняя ошибка сервера",
        Detail = exception?.Message,
        StatusCode = 500,
        Timestamp = DateTime.UtcNow
    };

    return Results.Json(result, statusCode: 500);
});

// Применение миграций при запуске
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SupplyChainContext>();
        context.Database.Migrate();

        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Миграции базы данных успешно применены");

        if (context.Database.CanConnect())
        {
            var userTableExists = context.Database.ExecuteSqlRaw(
                "SELECT name FROM sqlite_master WHERE type='table' AND name='Users'");

            if (userTableExists == 0)
            {
                logger.LogWarning("Таблица Users не найдена. Убедитесь, что миграции созданы правильно.");
            }
            else
            {
                logger.LogInformation("Таблица Users существует в базе данных");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Произошла ошибка при применении миграций");
    }
}

app.Run();