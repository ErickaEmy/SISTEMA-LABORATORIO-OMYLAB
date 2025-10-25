# ========== ETAPA 1: BUILD ==========
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
WORKDIR /src/SistemaLaboratorio
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# ========== ETAPA 2: RUNTIME ==========
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Instalar wkhtmltopdf desde repositorios oficiales de Debian
RUN apt-get update && apt-get install -y \
    wkhtmltopdf \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Verificar instalación
RUN wkhtmltopdf --version

# Copiar aplicación
COPY --from=build /app/out .

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaLaboratorio.dll"]