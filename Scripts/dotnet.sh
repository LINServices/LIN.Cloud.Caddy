sudo apt update
sudo apt install -y wget apt-transport-https

wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update

sudo apt install -y dotnet-runtime-9.0
sudo apt install -y aspnetcore-runtime-9.0

## INICIAR EL SERVICIO
APP_NAME="lin-cloud-caddy"
APP_DLL="LIN.Cloud.Caddy.dll"
APP_DIR="/var/www/api/publish"
APP_PORT=7059
SERVICE_USER="www-data"

echo "🔧 Instalando servicio $APP_NAME..."

# Crear directorio si no existe
sudo mkdir -p $APP_DIR

# Permisos
sudo chown -R $SERVICE_USER:$SERVICE_USER $APP_DIR

# Crear archivo de servicio
sudo tee /etc/systemd/system/$APP_NAME.service > /dev/null <<EOF
[Unit]
Description=LIN Cloud Caddy API
After=network.target

[Service]
WorkingDirectory=$APP_DIR
ExecStart=/usr/bin/dotnet $APP_DIR/$APP_DLL
Restart=always
RestartSec=5
User=$SERVICE_USER

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:$APP_PORT

SyslogIdentifier=$APP_NAME

[Install]
WantedBy=multi-user.target
EOF

# Recargar systemd
sudo systemctl daemon-reload

# Habilitar e iniciar servicio
sudo systemctl enable $APP_NAME
sudo systemctl start $APP_NAME

echo "✅ Servicio $APP_NAME instalado y ejecutándose"
echo "📄 Logs: journalctl -u $APP_NAME -f"

# Ver estado
systemctl status lin-cloud-caddy

# Reiniciar servicio
systemctl restart lin-cloud-caddy.service

sudo ufw allow $APP_PORT