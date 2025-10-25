# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Etapa final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Instalar dependencias necesarias para wkhtmltopdf
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
    wget

# Instalar wkhtmltopdf
RUN wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6-1/wkhtmltox_0.12.6-1.buster_amd64.deb \
    && apt install -y ./wkhtmltox_0.12.6-1.buster_amd64.deb \
    && rm wkhtmltox_0.12.6-1.buster_amd64.deb

# Copiar la app publicada
COPY --from=build /app/out .

# Rotativa busca el binario aquí por defecto
RUN mkdir -p /app/wwwroot/Rotativa
RUN cp /usr/local/bin/wkhtmltopdf /app/wwwroot/Rotativa/

# Puerto Railway
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaLaboratorio.dll"]
