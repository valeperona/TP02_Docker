# TP02_Docker - Minimal API .NET 8

API mínima desarrollada en .NET 8 con endpoints básicos para health check, echo y manejo de notas, extendida para trabajar con **Docker + PostgreSQL** en entornos QA y PROD.

## 📋 Requisitos

- .NET 8 SDK (para ejecución local)
- Docker Desktop / Docker Engine 24+
- (Opcional) `curl` y `jq` para pruebas

## 🚀 Instalación y ejecución local (sin Docker)

1. **Clonar el proyecto**
   ```bash
   git clone <repository-url>
   cd TP02_Docker
   ```

2. **Ejecutar el proyecto**
   ```bash
   dotnet run
   ```

3. **Acceso a la API**
   - HTTPS: `https://localhost:7000`
   - HTTP: `http://localhost:5000`
   - Swagger UI: `https://localhost:7000/swagger`

## 📡 Endpoints disponibles

### `GET /health`
Devuelve el estado de la API y el entorno actual.

**Respuesta:**
```json
{
  "status": "ok",
  "env": "dev"
}
```

### `GET /echo?msg=...`
Devuelve el mensaje recibido como parámetro.

**Ejemplo:**
```bash
# Request
GET /echo?msg=Hola mundo

# Response
{"message": "Hola mundo"}
```

### `GET /notes`
Obtiene todas las notas almacenadas.

**Respuesta:**
```json
[
  {
    "id": 1,
    "content": "Mi primera nota",
    "createdAt": "2024-01-15T10:30:00Z"
  }
]
```

### `POST /notes`
Crea una nueva nota.

**Request body:**
```json
{
  "content": "Contenido de la nota"
}
```

**Respuesta:**
```json
{
  "id": 1,
  "content": "Contenido de la nota",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### `GET /dbcheck`
Valida la conexión a PostgreSQL.

**Respuesta:**
```json
{"ok": true}
```

## ⚙️ Configuración de variables de entorno

### Modo local

**Windows:**
```cmd
set APP_ENV=production
dotnet run
```

**Linux/macOS:**
```bash
APP_ENV=production dotnet run
```

## 🐳 Docker - Construcción y ejecución

### Construir imagen manualmente

```bash
docker build -t tp02-docker-api .
docker run --rm -d --name tp02-api -p 8080:80 tp02-docker-api
```

La API estará disponible en `http://localhost:8080/health`.

### Imágenes en Docker Hub

- **`valeperona/tp02-docker-api:dev`** → Desarrollo
- **`valeperona/tp02-docker-api:v1.0`** → Release estable

### Docker Compose - Entornos QA y PROD

**Levantar infraestructura completa:**
```bash
docker compose up -d
```

**Servicios disponibles:**
- **QA:** `http://localhost:8081` (tp02-api-qa + tp02-db-qa)
- **PROD:** `http://localhost:8082` (tp02-api-prod + tp02-db-prod)

### Variables de entorno para Compose

Crear archivo `.env` (opcional):
```env
QA_DB_USER=postgres
QA_DB_PASS=postgres
QA_DB_NAME=notesqa

PROD_DB_USER=postgres
PROD_DB_PASS=postgres
PROD_DB_NAME=notesprod
```

## ✅ Verificación del TP

### 1. Verificar respuesta QA/PROD

```bash
# QA (env=qa)
curl -s http://localhost:8081/health | jq

# PROD (env=prod)  
curl -s http://localhost:8082/health | jq
```

### 2. Verificar conexión a BD

```bash
curl -s http://localhost:8081/dbcheck | jq
curl -s http://localhost:8082/dbcheck | jq
```

### 3. Verificar persistencia de datos

```bash
# Crear notas
curl -s -X POST "http://localhost:8081/notes?content=Nota%20QA"
curl -s -X POST "http://localhost:8082/notes?content=Nota%20PROD"

# Reiniciar servicios
docker compose down
docker compose up -d

# Verificar que las notas persisten
curl -s http://localhost:8081/notes | jq
curl -s http://localhost:8082/notes | jq
```

## 🛠 Administración de la base de datos

### Acceso a PostgreSQL QA

```bash
docker exec -it tp02-db-qa psql -U postgres -d notesqa
```

**Comandos útiles en psql:**
```sql
\dt                    -- Listar tablas
SELECT * FROM notes;   -- Ver todas las notas
\q                     -- Salir
```

### Acceso a PostgreSQL PROD

```bash
docker exec -it tp02-db-prod psql -U postgres -d notesprod
```

## 🏷️ Versionado de imágenes

**Convención:** SemVer (MAJOR.MINOR.PATCH)

- **`dev`** → Imagen mutable para desarrollo
- **`v1.0`** → Primer release estable

### Comandos de versionado

```bash
# Crear versión estable
docker tag valeperona/tp02-docker-api:dev valeperona/tp02-docker-api:v1.0
docker push valeperona/tp02-docker-api:v1.0

# Actualizar compose con nueva versión
docker compose down
docker compose up -d --pull always --force-recreate
```

## 📁 Estructura del proyecto

```
TP02_Docker/
├── docker-compose.yml     # Configuración multi-entorno
├── Dockerfile            # Multi-stage build
├── .env.example          # Variables de entorno ejemplo
├── MinimalApi.csproj     # Proyecto .NET 8
├── Program.cs            # Aplicación principal
└── README.md            # Este archivo
```

## 🎯 Objetivos cumplidos

- ✅ Aplicación containerizada con Docker
- ✅ Base de datos PostgreSQL en contenedor
- ✅ Persistencia de datos con volúmenes
- ✅ Entornos QA y PROD aislados
- ✅ Misma imagen para múltiples entornos
- ✅ Configuración via variables de entorno
- ✅ Docker Compose para reproducibilidad
- ✅ Healthchecks y dependencias
- ✅ Versionado semántico de imágenes