using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using sel_api_archivos.Api.Filters;
using sel_api_archivos.Api.Options;
using sel_api_archivos.Datos;
using sel_api_archivos.Negocio.Archivo;
using sel_api_archivos.Negocio.Storage;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "sel-api-archivos")
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

try
{
    Log.Information("Iniciando sel-api-archivos");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Vincular secciones de configuración a clases tipadas
    builder.Services.Configure<JwtSettingsOptions>(builder.Configuration.GetSection("JwtSettings"));
    builder.Services.Configure<ConnectionStringsOptions>(builder.Configuration.GetSection("ConnectionStrings"));
    builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));

    // Configuración de JWT usando opciones tipadas
    var jwtSection = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSection["Secret"] ?? throw new InvalidOperationException(
        "JwtSettings:Secret no está configurado en appsettings.json.");

    var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException(
        "JwtSettings:Issuer no está configurado en appsettings.json.");

    var audience = jwtSection["Audience"] ?? throw new InvalidOperationException(
        "JwtSettings:Audience no está configurado en appsettings.json.");

    // Add services to the container.
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<MidagriResponseFilter>();
    });

    // Configuración de endpoints e interactividad
    builder.Services.AddEndpointsApiExplorer();

    // Add CORS con orígenes desde opciones
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowUI", policy =>
        {
            var corsSection = builder.Configuration.GetSection("Cors:Origenes");
            var origins = corsSection.Get<string[]>() ?? new[] { "https://localhost:7100", "http://localhost:7100" };
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Configuración de Swagger con seguridad JWT
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "API de Archivos - AGROIDEAS",
            Version = "v1",
            Description = "Microservicio para la gestión de carga, descarga y lectura de archivos.",
            Contact = new OpenApiContact
            {
                Name = "Soporte Técnico",
                Email = "soporte@agroideas.gob.pe"
            }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Autenticación JWT usando el esquema Bearer. Ejemplo: 'Bearer {token}'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
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
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

    builder.Services.AddAuthorization();
    builder.Services.AddMemoryCache();

    // Registro de Inyección de Dependencias
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no está configurada en appsettings.json.");

    // Capa de Datos
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IArchivoRepositorio>(_ => new ArchivoRepositorio(connectionString));

    // Capa de Negocio (Estrategias de Almacenamiento)
    builder.Services.AddScoped<IStorageProvider, LocalStorageProvider>();
    builder.Services.AddScoped<IStorageProvider, FtpStorageProvider>();
    builder.Services.AddScoped<IStorageProviderResolver, StorageProviderResolver>();

    // Servicios de Coordinación
    builder.Services.AddScoped<IArchivoServicio, ArchivoServicio>();

    var app = builder.Build();

    // Servir Swagger en ambiente de desarrollo
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Archivos v1");
        });
    }

    app.UseCors("AllowUI");

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
