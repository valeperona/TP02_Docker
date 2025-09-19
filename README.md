# TP02_Docker
# Minimal API - .NET 8

API mínima desarrollada en .NET 8 con endpoints básicos para health check, echo y manejo de notas en memoria.

## Requisitos

- .NET 8 SDK

## Instalación y ejecución

1. Clona o descarga el proyecto
2. Navega al directorio del proyecto
3. Ejecuta el proyecto:

\`\`\`bash
dotnet run
\`\`\`

La API estará disponible en `https://localhost:7000` (HTTPS) y `http://localhost:5000` (HTTP).

## Endpoints disponibles

### GET /health
Devuelve el estado de la API y el entorno actual.

**Respuesta:**
\`\`\`json
{
  "status": "ok",
  "env": "dev"
}
\`\`\`

La variable de entorno `APP_ENV` se puede configurar para cambiar el valor del entorno (por defecto es "dev").

### GET /echo?msg=...
Devuelve el mensaje recibido como parámetro.

**Ejemplo:**
- Request: `GET /echo?msg=Hola mundo`
- Response: `{"message": "Hola mundo"}`

### GET /notes
Obtiene todas las notas almacenadas en memoria.

**Respuesta:**
\`\`\`json
[
  {
    "id": 1,
    "content": "Mi primera nota",
    "createdAt": "2024-01-15T10:30:00Z"
  }
]
\`\`\`

### POST /notes
Crea una nueva nota.

**Request body:**
\`\`\`json
{
  "content": "Contenido de la nota"
}
\`\`\`

**Respuesta:**
\`\`\`json
{
  "id": 1,
  "content": "Contenido de la nota",
  "createdAt": "2024-01-15T10:30:00Z"
}
\`\`\`

## Configuración de variables de entorno

Para configurar la variable `APP_ENV`:

**Windows:**
\`\`\`cmd
set APP_ENV=production
dotnet run
\`\`\`

**Linux/macOS:**
\`\`\`bash
APP_ENV=production dotnet run
\`\`\`

## Swagger UI

En modo desarrollo, puedes acceder a la documentación interactiva en:
- `https://localhost:7000/swagger`

## Notas importantes

- Las notas se almacenan en memoria y se pierden al reiniciar la aplicación
- No incluye autenticación ni autorización
- Configurado con CORS permisivo para desarrollo
- Listo para ser containerizado con Docker posteriormente
