# Informe Técnico — Actividad 3: TrabajoK8S
## Pipelines CI/CD con GitHub Actions y Jenkins

**Estudiante:** Esteban Giovanny Garay Cano  
**Programa:** Maestría en Arquitectura de Software y DevOps  
**Repositorio:** https://github.com/EstebanGarayCano/Actividad_Fundamentos_DevOps  
**Imagen Docker:** https://hub.docker.com/r/estebangaraycano/ecomify-customers  

---

## 1. Objetivo

Diseñar e implementar dos pipelines automatizados independientes — **GitHub Actions** para Integración Continua (CI) y **Jenkins** para Entrega Continua (CD) — que gestionen el ciclo de vida completo de una aplicación web de microservicios desplegada en un clúster de Kubernetes en Google Cloud (GKE).

---

## 2. Herramientas seleccionadas y justificación

| Herramienta | Rol | Justificación |
|-------------|-----|---------------|
| **GitHub Actions** | CI | Integración nativa con el repositorio GitHub, ejecución automática ante cada push/PR, sin infraestructura adicional |
| **Jenkins** | CD | Agnóstico del proveedor cloud, mayor control sobre el pipeline de entrega, estándar de la industria |
| **Docker** | Contenedores | Empaqueta la aplicación con todas sus dependencias, garantizando consistencia entre entornos |
| **DockerHub** | Registry | Gratuito, público, compatible con cualquier cluster Kubernetes sin configuración adicional |
| **xUnit + Moq** | Pruebas | Framework nativo .NET, integración directa con `dotnet test` y GitHub Actions |
| **GKE (Kubernetes)** | Orquestación | Escalabilidad automática, gestión declarativa de despliegues |
| **PostgreSQL (Supabase)** | Base de datos | Servicio gestionado, sin necesidad de administrar infraestructura de base de datos |

---

## 3. Arquitectura del sistema

```
┌─────────────────────────────────────────────────────────────┐
│                      GitHub Repository                       │
│   Frontend (React) · Backend · Productos · EcomifyCustomers  │
└─────────────────────┬───────────────────────────────────────┘
                      │ push / pull_request
            ┌─────────▼───────────┐
            │    GitHub Actions   │  ← Pipeline CI
            │    (ci.yml)         │
            │  1. Checkout        │
            │  2. dotnet restore  │
            │  3. dotnet build    │
            │  4. xUnit tests     │
            │  5. Upload results  │
            └─────────┬───────────┘
                      │ aprobado
            ┌─────────▼───────────┐
            │       Jenkins       │  ← Pipeline CD
            │    (Jenkinsfile)    │
            │  1. Checkout        │
            │  2. Docker build    │
            │  3. Push DockerHub  │
            │  4. Deploy K8s      │
            └─────────┬───────────┘
                      │
            ┌─────────▼───────────┐
            │  DockerHub Registry │
            │  estebangaraycano/  │
            │  ecomify-customers  │
            └─────────┬───────────┘
                      │
            ┌─────────▼───────────┐
            │  Google Kubernetes  │
            │  Engine (GKE)       │
            └─────────────────────┘
```

---

## 4. Microservicio EcomifyCustomers

### 4.1 Descripción

Microservicio ASP.NET Core 10 con CRUD completo sobre la tabla `ecommify_customers` en PostgreSQL. Implementa patrón de servicio (`ICustomerService`) para desacoplar la lógica de negocio y permitir pruebas unitarias sin base de datos real.

### 4.2 Conexión a la base de datos

El microservicio se conecta a **PostgreSQL en Supabase** mediante Entity Framework Core 10 con Npgsql:

```
Host:     db.whebvyxbbwlmtpkmiumn.supabase.co
Puerto:   5432
Base de datos: postgres
SSL:      Requerido
```

La tabla `ecommify_customers` contiene un tipo compuesto PostgreSQL (`address_type`) con los campos `zip_code`, `city` y `state`. Para resolver la incompatibilidad de EF Core con tipos compuestos se implementó:

- **Lectura:** entidad keyless `CustomerFlat` con `ToSqlQuery` que descompone el tipo usando la sintaxis SQL `(customer_address).campo`
- **Escritura:** SQL directo con `ExecuteSqlAsync` y el cast `ROW(...)::address_type`

### 4.3 Métodos del servicio

| Método | Descripción |
|--------|-------------|
| `GetAllAsync()` | Retorna todos los clientes de la tabla |
| `GetByIdAsync(string id)` | Retorna un cliente por su `customer_id` o `null` |
| `CreateAsync(CreateCustomerDto dto)` | Inserta un nuevo registro con dirección compuesta |
| `UpdateAsync(string id, UpdateCustomerDto dto)` | Actualiza `unique_id` y dirección de un cliente existente |
| `DeleteAsync(string id)` | Elimina un cliente; retorna `true` si se eliminó |

### 4.4 Endpoints REST

**Base URL local:** `http://localhost:8080`  
**Base URL Docker:** `http://localhost:8080`  
**Documentación Swagger:** `http://localhost:8080/swagger`

| Método | Endpoint | Descripción | Respuestas |
|--------|----------|-------------|------------|
| `GET` | `/api/customers` | Lista todos los clientes | `200 OK` |
| `GET` | `/api/customers/{id}` | Obtiene un cliente por ID | `200 OK` · `404 Not Found` |
| `POST` | `/api/customers` | Crea un nuevo cliente | `201 Created` · `409 Conflict` |
| `PUT` | `/api/customers/{id}` | Actualiza un cliente existente | `200 OK` · `404 Not Found` |
| `DELETE` | `/api/customers/{id}` | Elimina un cliente | `204 No Content` · `404 Not Found` |

**Ejemplo de body para POST:**
```json
{
  "customerId": "cliente-001",
  "customerUniqueId": "uid-abc123",
  "customerAddress": {
    "zipCode": "01310",
    "city": "São Paulo",
    "state": "SP"
  }
}
```

---

## 5. Docker

### 5.1 Configuración del Dockerfile

Se utiliza un build **multi-etapa** para minimizar el tamaño de la imagen final:

```dockerfile
# Etapa 1: compilar y publicar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["EcomifyCustomers.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Etapa 2: imagen de runtime (sin SDK)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "EcomifyCustomers.dll"]
```

### 5.2 Comandos de construcción y ejecución

```bash
# Construir la imagen
docker build -t ecomify-customers:latest \
  -f EcomifyCustomers/Dockerfile EcomifyCustomers

# Ejecutar el contenedor
docker run -d --name ecomify-customers \
  -p 8080:8080 ecomify-customers:latest
```

### 5.3 Imagen publicada en DockerHub

```bash
# Etiquetar para DockerHub
docker tag ecomify-customers:latest \
  estebangaraycano/ecomify-customers:latest

# Publicar
docker push estebangaraycano/ecomify-customers:latest

# Descargar desde cualquier máquina
docker pull estebangaraycano/ecomify-customers:latest
```

**URL de la imagen:** `docker.io/estebangaraycano/ecomify-customers:latest`  
**Digest:** `sha256:a1fe86868bd715c56011d7abab4f46fa0c546d3c410b9448c57181fe5abf1531`

### 5.4 Endpoints disponibles con el contenedor corriendo

| Recurso | URL |
|---------|-----|
| API base | `http://localhost:8080/api/customers` |
| Swagger UI | `http://localhost:8080/swagger` |

---

## 6. Pruebas unitarias con xUnit

### 6.1 Configuración

**Framework:** xUnit 2.9.3 + Moq 4.20.72  
**Proyecto:** `tests/EcomifyCustomers.Tests/`  
**Referencia:** apunta directamente al proyecto `EcomifyCustomers`

Todas las pruebas usan `Mock<ICustomerService>` — ninguna requiere base de datos real.

### 6.2 Pruebas creadas — 20 en total

#### `Models/CustomerAddressTests.cs` — 4 pruebas

| Prueba | Qué valida |
|--------|-----------|
| `CustomerAddress_Constructor_SetsAllProperties` | Constructor asigna `ZipCode`, `City` y `State` correctamente |
| `CustomerAddress_AllNullValues_CreatesValidRecord` | Acepta valores nulos sin excepción |
| `CustomerAddress_Equality_SameValuesMeansEqual` | Igualdad por valor entre dos records idénticos |
| `CustomerAddress_Equality_DifferentValuesMeansNotEqual` | Records con valores distintos no son iguales |

#### `Services/CustomerServiceContractTests.cs` — 6 pruebas

| Prueba | Qué valida |
|--------|-----------|
| `GetAllAsync_ReturnsEnumerableOfCustomerDto` | Retorna colección tipada correctamente |
| `GetByIdAsync_WithValidId_ReturnsCustomerDto` | Con ID válido retorna el DTO esperado |
| `GetByIdAsync_WithInvalidId_ReturnsNull` | Con ID inexistente retorna `null` |
| `CreateAsync_ReturnsCreatedCustomer` | El cliente creado coincide con el DTO recibido |
| `DeleteAsync_WithExistingId_ReturnsTrue` | Eliminar ID existente retorna `true` |
| `DeleteAsync_WithNonExistingId_ReturnsFalse` | Eliminar ID inexistente retorna `false` |

#### `Controllers/CustomersControllerTests.cs` — 10 pruebas

| Prueba | Qué valida |
|--------|-----------|
| `GetAll_ReturnsOk_WithListOfCustomers` | `GET /api/customers` → `200 OK` con lista |
| `GetAll_ReturnsOk_WithEmptyList` | `GET /api/customers` → `200 OK` con array vacío |
| `GetById_ExistingId_ReturnsOkWithCustomer` | `GET /api/customers/{id}` → `200 OK` |
| `GetById_NonExistingId_ReturnsNotFound` | `GET /api/customers/{id}` → `404` |
| `Create_NewCustomer_ReturnsCreated` | `POST` → `201 Created` |
| `Create_DuplicateId_ReturnsConflict` | `POST` ID duplicado → `409 Conflict` |
| `Update_ExistingCustomer_ReturnsOkWithUpdatedData` | `PUT` → `200 OK` con datos actualizados |
| `Update_NonExistingId_ReturnsNotFound` | `PUT` ID inexistente → `404` |
| `Delete_ExistingCustomer_ReturnsNoContent` | `DELETE` → `204 No Content` |
| `Delete_NonExistingId_ReturnsNotFound` | `DELETE` ID inexistente → `404` |

### 6.3 Ejecución de las pruebas

**Desde la terminal:**
```bash
dotnet test tests/EcomifyCustomers.Tests/ \
  --logger "console;verbosity=normal"
```

**Resultado obtenido:**
```
Test Run Successful.
Total tests: 20
     Passed: 20
 Total time: 0.4163 Seconds
```

---

## 7. Pipeline CI — GitHub Actions

### 7.1 Archivo de configuración

**Ruta:** `.github/workflows/ci.yml`

```yaml
name: CI — Unit Tests & Code Quality

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

jobs:
  unit-tests:
    name: xUnit Tests — EcomifyCustomers
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore tests/EcomifyCustomers.Tests/EcomifyCustomers.Tests.csproj

      - name: Build (no restore)
        run: dotnet build tests/EcomifyCustomers.Tests/EcomifyCustomers.Tests.csproj --no-restore -c Release

      - name: Run xUnit tests
        run: |
          dotnet test tests/EcomifyCustomers.Tests/EcomifyCustomers.Tests.csproj \
            --no-build -c Release \
            --logger "trx;LogFileName=results.trx" \
            --logger "console;verbosity=normal" \
            --results-directory ./TestResults \
            /p:CollectCoverage=true \
            /p:CoverletOutputFormat=opencover \
            /p:CoverletOutput=./TestResults/coverage.xml

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results-ecomify-customers
          path: TestResults/
```

### 7.2 Stages del pipeline CI

| # | Stage | Comando | Propósito |
|---|-------|---------|-----------|
| 1 | **Checkout** | `actions/checkout@v4` | Clona el repositorio completo en el runner |
| 2 | **Setup .NET 10** | `actions/setup-dotnet@v4` | Instala el SDK y dependencias del runtime |
| 3 | **Restore dependencies** | `dotnet restore` | Descarga los paquetes NuGet |
| 4 | **Build** | `dotnet build -c Release` | Compila en modo Release |
| 5 | **Run xUnit tests** | `dotnet test` | Ejecuta 20 pruebas, genera TRX y cobertura OpenCover |
| 6 | **Upload artifacts** | `actions/upload-artifact@v4` | Guarda resultados descargables en GitHub |

### 7.3 Ejecución automática

El pipeline se dispara automáticamente ante:
- **Push** a la rama `main`
- **Pull Request** hacia `main`
- Ejecución manual desde GitHub UI (`workflow_dispatch`)

---

## 8. Pipeline CD — Jenkins

### 8.1 Archivo de configuración

**Ruta:** `Jenkinsfile` (raíz del repositorio)

```groovy
pipeline {
    agent any

    environment {
        DOCKERHUB_USER    = 'estebangaraycano'
        IMAGE_NAME        = 'ecomify-customers'
        IMAGE_TAG         = "latest"
        DOCKERHUB_CREDS   = credentials('dockerhub-credentials')
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Docker Image') {
            steps {
                sh """
                    docker build \
                      -t ${DOCKERHUB_USER}/${IMAGE_NAME}:${IMAGE_TAG} \
                      -f EcomifyCustomers/Dockerfile \
                      EcomifyCustomers
                """
            }
        }

        stage('Push to DockerHub') {
            steps {
                sh """
                    echo ${DOCKERHUB_CREDS_PSW} | \
                      docker login -u ${DOCKERHUB_CREDS_USR} --password-stdin
                    docker push ${DOCKERHUB_USER}/${IMAGE_NAME}:${IMAGE_TAG}
                """
            }
        }

        stage('Deploy to Kubernetes') {
            steps {
                sh """
                    kubectl set image deployment/ecomify-customers \
                      ecomify-customers=${DOCKERHUB_USER}/${IMAGE_NAME}:${IMAGE_TAG} \
                      --record
                    kubectl rollout status deployment/ecomify-customers --timeout=120s
                """
            }
        }
    }

    post {
        success {
            echo "Pipeline CD completado. Imagen desplegada: ${DOCKERHUB_USER}/${IMAGE_NAME}:${IMAGE_TAG}"
        }
        failure {
            echo "Pipeline CD falló. Revisar logs."
        }
    }
}
```

### 8.2 Stages del pipeline CD

| # | Stage | Propósito |
|---|-------|-----------|
| 1 | **Checkout** | Clona el repositorio desde GitHub |
| 2 | **Build Docker Image** | Construye la imagen con el Dockerfile multi-etapa |
| 3 | **Push to DockerHub** | Autentica y publica en `estebangaraycano/ecomify-customers` |
| 4 | **Deploy to Kubernetes** | Actualiza el deployment en GKE con la nueva imagen |

---

## 9. Estructura final del repositorio

```
Actividad3-TrabajoK8S/
├── .github/
│   └── workflows/
│       ├── ci.yml                       ← Pipeline CI (GitHub Actions)
│       └── build-push-deploy.yml        ← Build + deploy GKE
├── EcomifyCustomers/
│   ├── Controllers/
│   │   └── CustomersController.cs       ← Endpoints REST
│   ├── Data/
│   │   └── AppDbContext.cs              ← EF Core + ToSqlQuery
│   ├── DTOs/
│   │   ├── CustomerDto.cs
│   │   ├── CreateCustomerDto.cs
│   │   └── UpdateCustomerDto.cs
│   ├── Models/
│   │   ├── Customer.cs
│   │   └── CustomerAddress.cs           ← Tipo compuesto PostgreSQL
│   ├── Services/
│   │   ├── ICustomerService.cs          ← Interfaz (contrato)
│   │   └── CustomerService.cs           ← Implementación EF Core
│   ├── appsettings.json                 ← Connection string Supabase
│   ├── Dockerfile                       ← Build multi-etapa
│   └── Program.cs                       ← DI + Npgsql + Swagger
├── tests/
│   └── EcomifyCustomers.Tests/
│       ├── Controllers/
│       │   └── CustomersControllerTests.cs  ← 10 pruebas
│       ├── Models/
│       │   └── CustomerAddressTests.cs      ← 4 pruebas
│       ├── Services/
│       │   └── CustomerServiceContractTests.cs ← 6 pruebas
│       └── EcomifyCustomers.Tests.csproj
├── Backend/                             ← Microservicio autenticación
├── Productos/                           ← Microservicio productos
├── Frontend/                            ← App React
├── helm/                                ← Chart Helm para K8s
├── Jenkinsfile                          ← Pipeline CD
├── INFORME_TECNICO.md                   ← Este documento
├── DOCUMENTACION.md
└── README.md
```

---

## 10. Resumen de entregables

| Entregable | Estado | Evidencia |
|------------|--------|-----------|
| Microservicio EcomifyCustomers (CRUD + PostgreSQL) | ✅ Completo | Código en repositorio |
| Dockerfile multi-etapa | ✅ Completo | `EcomifyCustomers/Dockerfile` |
| Imagen publicada en DockerHub | ✅ Completo | `estebangaraycano/ecomify-customers:latest` |
| 20 pruebas unitarias xUnit pasando | ✅ Completo | `tests/EcomifyCustomers.Tests/` |
| Pipeline CI — GitHub Actions (`ci.yml`) | ✅ Completo | `.github/workflows/ci.yml` |
| Ejecución automática en push/PR | ✅ Completo | GitHub Actions History |
| Pipeline CD — Jenkins (`Jenkinsfile`) | ✅ Completo | `Jenkinsfile` |
| README con flujo CI/CD | ✅ Completo | `README.md` |
| Documentación técnica | ✅ Completo | Este documento |
