# ========== ETAPA 1: BUILD ==========
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia todo el código al contenedor
COPY . .

# Entra en la carpeta del proyecto principal
WORKDIR /src/SistemaLaboratorio

# Restaura dependencias y publica en modo Release
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# ========== ETAPA 2: RUNTIME CON ROTATIVA ==========
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# ⚠️ IMPORTANTE: Instalar wkhtmltopdf para Rotativa
RUN apt-get update && apt-get install -y \
    # Dependencias de wkhtmltopdf
    wget \
    fontconfig \
    libfreetype6 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    xfonts-75dpi \
    xfonts-base \
    libjpeg62-turbo \
    libpng16-16 \
    # Descarga e instala wkhtmltopdf
    && wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-2/wkhtmltox_0.12.6.1-2.bullseye_amd64.deb \
    && dpkg -i wkhtmltox_0.12.6.1-2.bullseye_amd64.deb || apt-get install -y -f \
    && rm wkhtmltox_0.12.6.1-2.bullseye_amd64.deb \
    # Limpia archivos temporales
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Verifica que wkhtmltopdf está instalado
RUN wkhtmltopdf --version

# Copia los archivos publicados
COPY --from=build /app/out .

# Puerto dinámico para Railway
ENV PORT=8080
EXPOSE 8080

# Inicia la aplicación
ENTRYPOINT ["dotnet", "SistemaLaboratorio.dll"]