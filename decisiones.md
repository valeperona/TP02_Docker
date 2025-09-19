# Decisiones de Diseño - Docker TP02

## Parte 1: Elección y preparación de la aplicación

### Aplicación elegida
- **Framework:** Minimal API en .NET 8 (LTS)
- **Endpoints implementados:**
  - `GET /health` → estado del servicio + entorno (`APP_ENV`)
  - `GET /dbcheck` → validación de conexión a la base (SELECT 1)
  - `POST/GET /notes` → CRUD mínimo para demostrar persistencia

### Justificación de .NET 8 Minimal API
- **Simplicidad:** Suficiente para cubrir networking, variables de entorno, volúmenes y despliegues
- **LTS:** Estabilidad y soporte oficial a largo plazo
- **Ecosistema:** Cliente PostgreSQL (`Npgsql`) maduro y bien documentado

### Repositorio y versionado
- Fork propio trabajando en branch `TP02_Docker`
- Asegura trazabilidad de cambios y CI/CD futuro

### Entorno Docker
- **Plataforma:** Docker Desktop (macOS)
- **Verificación:** `docker version`, `docker ps`
- **Justificación:** GUI + CLI útil para defensa y monitoreo

## Parte 2: Construcción de imagen personalizada

### Dockerfile multi-stage
```dockerfile
# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MinimalApi.csproj ./
RUN dotnet restore MinimalApi.csproj

COPY . .
RUN dotnet publish MinimalApi.csproj -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80 443
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MinimalApi.dll"]
```

### Decisiones de diseño del Dockerfile
- **Multi-stage:** Compilo en `sdk` y ejecuto en `aspnet` → imagen final más liviana y segura
- **Cacheo del restore:** Copio primero el `.csproj` para reutilizar caché entre builds
- **ASPNETCORE_URLS:** La app escucha en `0.0.0.0:80` (requisito para contenedores)
- **ENTRYPOINT:** Proceso principal inmutable; parámetros podrían venir por `CMD` si fuese necesario

## Parte 3: Publicación en Docker Hub

### Estrategia de tags
```bash
# Tag de desarrollo
docker tag tp02-docker-api valeperona/tp02-docker-api:dev
docker push valeperona/tp02-docker-api:dev

# Versión estable
docker tag valeperona/tp02-docker-api:dev valeperona/tp02-docker-api:v1.0
docker push valeperona/tp02-docker-api:v1.0
```

### Justificación de estrategia
- **`dev`:** Imagen mutable para iterar rápido durante desarrollo
- **`v1.0`:** Release estable para QA/PROD con versionado semántico

## Parte 4: Base de datos en contenedor + persistencia

### Base de datos elegida
- **Engine:** PostgreSQL 16 (alpine)
- **Justificación:** Open-source, liviana, excelente soporte en .NET (Npgsql), estándar en la industria

### Configuración de red y persistencia
```bash
docker network create tp02-net
docker volume create pgdata

docker run -d --name tp02-db \
  --network tp02-net \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=notesdb \
  -v pgdata:/var/lib/postgresql/data \
  postgres:16-alpine
```

### Decisiones de seguridad y arquitectura
- **Red de usuario (`tp02-net`):** Para resolver `tp02-db` por nombre
- **Volumen nombrado (`pgdata`):** Persistir datos entre reinicios
- **Puerto no expuesto:** La API se comunica por red interna → mayor seguridad
- **Variables de entorno:** Configuración inyectada para flexibilidad

## Parte 5: QA y PROD con la misma imagen

### Objetivo
Ejecutar dos entornos simultáneos con la misma imagen; solo cambia la configuración (12-Factor App)

### Estrategia de aislamiento
- **Misma imagen:** `valeperona/tp02-docker-api` para QA/PROD → inmutabilidad
- **Variables diferenciadas por entorno:**
  - `APP_ENV` (qa/prod)
  - `LOG_LEVEL` (debug/warning)  
  - `ConnectionStrings__Default` (cadenas distintas por entorno)
- **Aislamiento de red:** Redes y volúmenes separados por entorno

### Implementación
```bash
# Redes y volúmenes separados
docker network create tp02-qa
docker network create tp02-prod
docker volume create pgdata_qa
docker volume create pgdata_prod

# Bases de datos independientes
docker run -d --name tp02-db-qa --network tp02-qa \
  -e POSTGRES_DB=notesqa \
  -v pgdata_qa:/var/lib/postgresql/data postgres:16-alpine

docker run -d --name tp02-db-prod --network tp02-prod \
  -e POSTGRES_DB=notesprod \
  -v pgdata_prod:/var/lib/postgresql/data postgres:16-alpine
```

## Parte 6: Entorno reproducible con docker-compose

### Arquitectura del docker-compose.yml
```yaml
services:
  db-qa:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: ${QA_DB_USER:-postgres}
      POSTGRES_PASSWORD: ${QA_DB_PASS:-postgres}
      POSTGRES_DB: ${QA_DB_NAME:-notesqa}
    volumes:
      - pgdata_qa:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d notesqa"]
      interval: 5s
      timeout: 3s
      retries: 15

  api-qa:
    image: valeperona/tp02-docker-api:v1.0
    depends_on:
      db-qa:
        condition: service_healthy
    environment:
      APP_ENV: qa
      LOG_LEVEL: debug
      ConnectionStrings__Default: Host=db-qa;Username=postgres;Password=postgres;Database=notesqa
    ports:
      - "8081:80"
```

### Decisiones de reproducibilidad
- **Versiones fijas:** `postgres:16-alpine`, `tp02-docker-api:v1.0`
- **Declarativo:** `docker compose up -d` levanta lo mismo en cualquier máquina
- **Healthchecks:** Sincronizan arranque de servicios dependientes
- **Sin dependencias locales:** No necesito .NET ni Postgres instalados en el host
- **Variables de entorno:** Archivo `.env` para configuración externa

## Parte 7: Versión etiquetada

### Convención de versionado
- **Estándar:** SemVer (MAJOR.MINOR.PATCH)
- **`v1.0`:** Primer release estable
- **`dev`:** Línea mutable para desarrollo continuo

### Proceso de release
```bash
docker tag valeperona/tp02-docker-api:dev valeperona/tp02-docker-api:v1.0
docker push valeperona/tp02-docker-api:v1.0

# Actualización de compose
docker compose down
docker compose up -d --pull always --force-recreate
```

## Problemas encontrados y soluciones

### Problemas técnicos resueltos
1. **Conflicto de puertos DB (5433 ocupado)** 
   - Solución: Dejé la DB sin exponer puertos; la API usa red interna

2. **Nombres de contenedor duplicados** 
   - Solución: `docker rm -f <nombre>` o no fijar `container_name` en Compose

3. **MSB1011 en publish** 
   - Solución: El directorio tenía más de un proyecto; invoqué `dotnet publish MinimalApi.csproj`

4. **API arrancaba antes que DB** 
   - Solución: `healthcheck + depends_on.condition: service_healthy`

### Verificación de funcionalidad
- Respuesta correcta de `/health`, `/dbcheck`
- Persistencia de datos tras reinicio de contenedores
- Recreación controlada con Compose
- Aislamiento entre entornos QA y PROD
