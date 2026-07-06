using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using sel_api_archivos.Api.Filters;
using sel_api_archivos.Datos;
using sel_api_archivos.Negocio.Archivo;
using sel_api_archivos.Negocio.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Agregar el filtro global para respuestas estándar MIDAGRI y manejo de excepciones
    options.Filters.Add<MidagriResponseFilter>();
});

// Configuración de endpoints e interactividad
builder.Services.AddEndpointsApiExplorer();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() 
                      ?? new[] { "https://localhost:7100", "http://localhost:7100" };
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

    // Configurar definición de seguridad JWT para Swagger UI
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

// Configuración de JWT compatible con sel-api-seguridad
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];

if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException(
        "JWT Secret is not configured. Set 'JwtSettings:Secret' in appsettings.json.");
}

var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

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

// Registro de Inyección de Dependencias
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("DefaultConnection string not found.");

// Capa de Datos
builder.Services.AddScoped<IArchivoRepositorio>(sp => new ArchivoRepositorio(connectionString));

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
