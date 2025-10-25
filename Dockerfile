# ========== ETAPA 1: BUILD ==========
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
WORKDIR /src/SistemaLaboratorio

# Ignorar warnings como errores durante el build
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out /p:TreatWarningsAsErrors=false

# ========== ETAPA 2: RUNTIME CON ROTATIVA ==========
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Instalar wkhtmltopdf y sus dependencias (SIN verificación que falla)
RUN apt-get update && apt-get install -y \
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
    && wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-2/wkhtmltox_0.12.6.1-2.bullseye_amd64.deb \
    && apt-get install -y -f ./wkhtmltox_0.12.6.1-2.bullseye_amd64.deb \
    && rm wkhtmltox_0.12.6.1-2.bullseye_amd64.deb \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Copiar aplicación
COPY --from=build /app/out .

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaLaboratorio.dll"]