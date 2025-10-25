#!/bin/bash

echo "Instalando wkhtmltopdf..."

# Actualizar repositorios
apt-get update

# Instalar dependencias
apt-get install -y \
    libssl-dev \
    libxrender1 \
    libxext6 \
    libfontconfig1 \
    fontconfig \
    xfonts-base \
    xfonts-75dpi \
    wget

# Descargar e instalar wkhtmltopdf
cd /tmp
wget https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6-1/wkhtmltox_0.12.6-1.bionic_amd64.deb
apt install -y /tmp/wkhtmltox_0.12.6-1.bionic_amd64.deb

echo "wkhtmltopdf instalado correctamente"

# Verificar instalación
which wkhtmltopdf
wkhtmltopdf --version