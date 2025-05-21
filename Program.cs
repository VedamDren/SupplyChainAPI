using Microsoft.EntityFrameworkCore;
using SupplyChainData;
using SupplyChainAPI.Mappings;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ��������� �����������
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// ��������� ����������� � ��
builder.Services.AddDbContext<SupplyChainContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ����������� AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ����������� �������� � ������������
builder.Services.AddControllers();

// ��������� Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Supply Chain API",
        Version = "v1",
        Description = "API ��� ������������� ������������ ������� ��������"
    });
});

var app = builder.Build();

// ������������ ��������� ��������
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Supply Chain API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();

// ���������� �������� ��� �������
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SupplyChainContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "��������� ������ ��� ���������� ��������");
    }
}

app.Run();