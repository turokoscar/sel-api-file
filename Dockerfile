# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY *.slnx .
COPY Directory.Build.props .
COPY sel-api-archivos.Api/sel-api-archivos.Api.csproj sel-api-archivos.Api/
COPY sel-api-archivos.Negocio/sel-api-archivos.Negocio.csproj sel-api-archivos.Negocio/
COPY sel-api-archivos.Datos/sel-api-archivos.Datos.csproj sel-api-archivos.Datos/
COPY sel-api-archivos.Entidad/sel-api-archivos.Entidad.csproj sel-api-archivos.Entidad/

RUN dotnet restore sel-api-archivos.Api/sel-api-archivos.Api.csproj

COPY . .
RUN dotnet build sel-api-archivos.Api/sel-api-archivos.Api.csproj -c Release -o /app/build
RUN dotnet publish sel-api-archivos.Api/sel-api-archivos.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Configurar zona horaria de Perú
RUN apt-get update && apt-get install -y --no-install-recommends tzdata \
    && ln -sf /usr/share/zoneinfo/America/Lima /etc/localtime \
    && echo "America/Lima" > /etc/timezone \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd -r appgroup && useradd -r -g appgroup appuser

COPY --from=build /app/publish .

# Crear directorio de almacenamiento para archivos cargados y dar permisos
RUN mkdir -p /app/storage && chown -R appuser:appgroup /app/storage

RUN chown -R appuser:appgroup /app
USER appuser

# Connection string via environment variable (sobreescribe appsettings.json)
ENV ASPNETCORE_ConnectionStrings__DefaultConnection="Server=sql-server;Database=BD_API_FILE;User Id=usr_api_file_app;Password=${DB_PASSWORD};TrustServerCertificate=True;"

EXPOSE 8080

ENTRYPOINT ["dotnet", "sel-api-archivos.Api.dll"]
