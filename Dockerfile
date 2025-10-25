# Usa la imagen base del SDK de .NET 8 (cambia si tu proyecto usa .NET 6 o 7)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia todo el código al contenedor
COPY . .

# Entra en la carpeta del proyecto principal
WORKDIR /src/SistemaLaboratorio

# Restaura dependencias y publica en modo Release
RUN dotnet restore
RUN dotnet publish -c Release -o /app/out

# Imagen final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Inicia la aplicación
ENTRYPOINT ["dotnet", "SistemaLaboratorio.dll"]
