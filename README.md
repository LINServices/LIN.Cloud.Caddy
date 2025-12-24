# LIN.Cloud.Caddy

Controlador inteligente para Caddy Server con persistencia en base de datos. Este proyecto permite gestionar rutas dinámicamente a través de una API segura, garantizando que la configuración se mantenga incluso después de reinicios.

## 🚀 Características

- **Gestión Dinámica de Rutas**: Registro y eliminación de rutas mediante API.
- **Persistencia Robusta**: Sincronización automática con SQL Server mediante EF Core.
- **Restauración Atómica**: Estrategia de "Intergalactic Restore" usando el endpoint `/load` de Caddy para reemplazos de configuración seguros.
- **Seguridad**: Autenticación basada en Master Key y API Keys.
- **Monitoreo de Versión**: Seguimiento de la versión del proyecto y de la instancia de Caddy conectada.

## 🛠️ Arquitectura

La solución está dividida en cuatro componentes principales:

- **`LIN.Cloud.Caddy`**: Capa de API Web y controladores.
- **`LIN.Cloud.Caddy.Persistence`**: Capa de acceso a datos (DbContext, Repositorios).
- **`LIN.Cloud.Caddy.Services`**: Lógica de negocio e integración directa con la API de Caddy.
- **`LIN.Cloud.Caddy.Test`**: Suite de pruebas unitarias para garantizar la estabilidad.

## ⚙️ Configuración

Asegúrate de configurar los siguientes valores en tu `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Tu-Cadena-De-Conexión"
  },
  "Caddy": {
    "AdminBaseUrl": "http://127.0.0.1:2019"
  },
  "Authentication": {
    "MasterKey": "tu-clave-maestra",
    "DefaultApiKey": "tu-api-key-por-defecto"
  }
}
```

## 🚥 Endpoints Principales

- `POST /api/client/register`: Registra una nueva ruta (Host + Puerto).
- `DELETE /api/client/remove/{id}`: Elimina una ruta por su ID.
- `POST /api/client/restore`: Sincroniza todas las rutas de la DB con Caddy.
- `GET /api/client/version`: Obtiene la versión del proyecto y de Caddy.

## 🧪 Pruebas

Para ejecutar las pruebas unitarias:

```powershell
dotnet test
```

## 👨‍💻 Autor

- **Alexander Giraldo** - *LIN Cloud Lake*
