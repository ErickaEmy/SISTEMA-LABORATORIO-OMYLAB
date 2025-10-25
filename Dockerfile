# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos solo los archivos del proyecto
COPY SistemaLaboratorio/SistemaLaboratorio.csproj SistemaLaboratorio/
RUN dotnet restore SistemaLaboratorio/SistemaLaboratorio.csproj

# Copiamos todo el código
COPY . .

# Publicamos la app
RUN dotnet publish SistemaLaboratorio/SistemaLaboratorio.csproj -c Release -o /app/out

# Etapa final
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Instalar dependencias necesarias y wkhtmltopdf (compatible con Debian 12)
RUN apt-get update && apt-get install -y \
    fontconfig \
    libfreetype6 \
    libjpeg62-turbo \
    libpng16-16 \
    libx11-6 \
    libxcb1 \
    libxext6 \
    libxrender1 \
    xfonts-75dpi \
    xfonts-base \
    wkhtmltopdf \
    && rm -rf /var/lib/apt/lists/*

# Copiar la app publicada
COPY --from=build /app/out .

# Crear carpeta para Rotativa y copiar el binario
RUN mkdir -p /app/wwwroot/Rotativa \
    && cp /usr/bin/wkhtmltopdf /app/wwwroot/Rotativa/

# Puerto para Railway
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaLaboratorio.dll"]
